namespace Flake
{
  using System;

  public class IdWorker
  {
    public const long Twepoch = 1288834974657L;

    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

    private const int WorkerIdShift = SequenceBits;
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    public const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    private const long SequenceMask = -1L ^ (-1L << SequenceBits);

    private long _lastTimestamp = -1L;

    public IdWorker(long workerId, long datacenterId, long sequence = 0L)
    {
      WorkerId = workerId;
      DatacenterId = datacenterId;
      Sequence = sequence;

      // sanity check for workerId
      if (workerId > MaxWorkerId || workerId < 0)
      {
        throw new ArgumentException(string.Format("worker Id can't be greater than {0} or less than 0", MaxWorkerId));
      }

      if (datacenterId > MaxDatacenterId || datacenterId < 0)
      {
        throw new ArgumentException(string.Format("datacenter Id can't be greater than {0} or less than 0",
          MaxDatacenterId));
      }

      //log.info(
      //    String.Format("worker starting. timestamp left shift {0}, datacenter id bits {1}, worker id bits {2}, sequence bits {3}, workerid {4}",
      //                  TimestampLeftShift, DatacenterIdBits, WorkerIdBits, SequenceBits, workerId)
      //    );	
    }

    public long WorkerId { get; protected set; }

    public long DatacenterId { get; protected set; }

    public long Sequence { get; set; }

    // def get_timestamp() = System.currentTimeMillis

    private readonly object _lock = new object();

    public virtual long NextId()
    {
      lock (_lock)
      {
        var timestamp = TimeGen();

        if (timestamp < _lastTimestamp)
        {
          //exceptionCounter.incr(1);
          //log.Error("clock is moving backwards.  Rejecting requests until %d.", _lastTimestamp);
          throw new InvalidSystemClockException(string.Format(
            "Clock moved backwards.  Refusing to generate id for {0} milliseconds", _lastTimestamp - timestamp));
        }

        if (_lastTimestamp == timestamp)
        {
          Sequence = (Sequence + 1) & SequenceMask;
          if (Sequence == 0)
          {
            timestamp = TilNextMillis(_lastTimestamp);
          }
        }
        else
        {
          Sequence = 0;
        }

        _lastTimestamp = timestamp;
        var id = ((timestamp - Twepoch) << TimestampLeftShift) |
                 (DatacenterId << DatacenterIdShift) |
                 (WorkerId << WorkerIdShift) | Sequence;

        return id;
      }
    }

    protected virtual long TilNextMillis(long lastTimestamp)
    {
      var timestamp = TimeGen();
      while (timestamp <= lastTimestamp)
      {
        timestamp = TimeGen();
      }
      return timestamp;
    }

    protected virtual long TimeGen()
    {
      return TimeExtenions.CurrentTimeMillis();
    }
  }
}