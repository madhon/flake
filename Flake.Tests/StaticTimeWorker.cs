namespace Flake.Tests
{
    using System;

    internal class StaticTimeWorker : IdWorker
    {
        public StaticTimeWorker(long workerId, long datacenterId, long sequence = 0)
            : base(workerId, datacenterId, sequence)
        {
        }

        protected override long TimeGen()
        {
            return Time + Twepoch;
        }

        public long Time { get; set; } = 1L;
    }
}