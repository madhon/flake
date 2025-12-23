namespace Flake;

using System;
using System.Threading;


public static class TimeExtensions
{
    private static readonly TimeProvider CurrentTimeProvider = TimeProvider.System;
    private static volatile Func<long> currentTimeFunc = InternalCurrentTimeMillis;

    public static long CurrentTimeMillis() => currentTimeFunc();

    public static IDisposable StubCurrentTime(Func<long> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        // Swap atomically to be safe if used across threads
        var previous = Interlocked.Exchange(ref currentTimeFunc, func);

        return new DisposableAction(() => Interlocked.Exchange(ref currentTimeFunc, previous));
    }

    public static IDisposable StubCurrentTime(long millis)
    {
        return StubCurrentTime(() => millis);
    }

    private static long InternalCurrentTimeMillis() =>
        CurrentTimeProvider.GetUtcNow().ToUnixTimeMilliseconds();
}