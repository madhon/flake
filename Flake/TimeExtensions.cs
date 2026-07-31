namespace Flake;

using System;
using System.Threading;


public static class TimeExtensions
{
    private static volatile Func<long>? _stub;
    
    public static long CurrentTimeMillis()
    {
        var stub = _stub;
        return stub is not null
            ? stub()
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static IDisposable StubCurrentTime(Func<long> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        var previous = Interlocked.Exchange(ref _stub, func);
        return new DisposableAction(() => Interlocked.Exchange(ref _stub, previous));
    }

    public static IDisposable StubCurrentTime(long millis)
    {
        return StubCurrentTime(() => millis);
    }
}