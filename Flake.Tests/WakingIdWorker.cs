namespace Flake.Tests
{
  internal class WakingIdWorker : IdWorker
  {
    private int _slept = 0;

    public WakingIdWorker(long workerId, long datacenterId, long sequence = 0) : base(workerId, datacenterId, sequence)
    {
    }

    protected override long TilNextMillis(long lastTimestamp)
    {
      _slept++;
      return base.TilNextMillis(lastTimestamp);
    }

    public int Slept
    {
      get { return _slept; }
    }
  }
}
