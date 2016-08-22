namespace Flake.Tests
{
    using System;

    internal class WakingIdWorker : IdWorker
    {
        public WakingIdWorker(long workerId, long datacenterId, long sequence = 0)
            : base(workerId, datacenterId, sequence)
        {
        }

        protected override long TilNextMillis(long lastTimestamp)
        {
            Slept++;
            return base.TilNextMillis(lastTimestamp);
        }

        public int Slept { get; private set; }
    }
}