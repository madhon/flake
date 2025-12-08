namespace Flake;

using System;

public static class TimeExtensions
{
    private static TimeProvider currentTimeProvider = TimeProvider.System;

    private static Func<long> currentTimeFunc = InternalCurrentTimeMillis;

    public static long CurrentTimeMillis() => currentTimeFunc();

    public static IDisposable StubCurrentTime(Func<long> func)
    {
        currentTimeFunc = func;
        return new DisposableAction(() => currentTimeFunc = InternalCurrentTimeMillis);
    }

    public static IDisposable StubCurrentTime(long millis)
    {
        currentTimeFunc = () => millis;
        return new DisposableAction(() => currentTimeFunc = InternalCurrentTimeMillis);
    }

    private static long InternalCurrentTimeMillis() => currentTimeProvider.GetUtcNow().ToUnixTimeMilliseconds();
}