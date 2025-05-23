﻿namespace Flake;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Represents an ID generator using the Snowflake algorithm, which generates unique IDs
/// based on a combination of timestamp, datacenter ID, worker ID, and sequence number.
/// </summary>
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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="workerId">Worker ID</param>
    /// <param name="datacenterId">Datacenter ID</param>
    /// <param name="sequence">Starting Sequence Number</param>
    public IdWorker(long workerId, long datacenterId, long sequence = 0L)
    {
        WorkerId = workerId;
        DatacenterId = datacenterId;
        Sequence = sequence;

        // sanity check for workerId
        if (workerId is > MaxWorkerId or < 0)
        {
            throw new ArgumentException($"worker Id can't be greater than {MaxWorkerId.ToString()} or less than 0", nameof(workerId));
        }

        if (datacenterId is > MaxDatacenterId or < 0)
        {
            throw new ArgumentException($"datacenter Id can't be greater than {MaxDatacenterId.ToString()} or less than 0", nameof(datacenterId));
        }
    }

    public long WorkerId { get; protected set; }

    public long DatacenterId { get; protected set; }

    public long Sequence { get; set; }

    // def get_timestamp() = System.currentTimeMillis
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new ();
#else
        private readonly object _lock = new object();
#endif
    /// <summary>
    /// Generates and returns the next unique ID.
    /// </summary>
    /// <returns>Next ID</returns>
    /// <exception cref="InvalidSystemClockException">When the clock is moving backwards</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual long NextId()
    {
        lock (_lock)
        {
            var timestamp = TimeGen();

            if (timestamp < _lastTimestamp)
            {
                throw new InvalidSystemClockException(
                    $"Clock moved backwards.  Refusing to generate id for {(_lastTimestamp - timestamp).ToString(CultureInfo.InvariantCulture)} milliseconds");
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

    protected virtual long TimeGen() => TimeExtensions.CurrentTimeMillis();
}