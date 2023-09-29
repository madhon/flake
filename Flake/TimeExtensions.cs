namespace Flake
{
    using System;

    public static class TimeExtensions
    {
        private static Func<long> currentTimeFunc = InternalCurrentTimeMillis;

        public static long CurrentTimeMillis() => currentTimeFunc();

        public static IDisposable StubCurrentTime(Func<long> func)
        {
            currentTimeFunc = func;
            return new DisposableAction(() => { currentTimeFunc = InternalCurrentTimeMillis; });
        }

        public static IDisposable StubCurrentTime(long millis)
        {
            currentTimeFunc = () => millis;
            return new DisposableAction(() => { currentTimeFunc = InternalCurrentTimeMillis; });
        }

#if NETSTANDARD2_0
        private static readonly DateTime Jan1st1970 = new DateTime
            (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long InternalCurrentTimeMillis() => (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
#endif

#if NET6_0_OR_GREATER
    private static long InternalCurrentTimeMillis() => (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
#endif

    }
}