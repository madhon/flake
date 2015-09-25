namespace Flake.Tests
{
  internal class StaticTimeWorker :IdWorker
  {
    private long _time = 1L;

    public StaticTimeWorker(long workerId, long datacenterId, long sequence = 0)
        : base(workerId, datacenterId, sequence)
    {
    }

    protected override long TimeGen()
    {
      return _time + IdWorker.Twepoch;
    }

    public long Time
    {
      get { return _time; }
      set { _time = value; }
    }
  }
}
