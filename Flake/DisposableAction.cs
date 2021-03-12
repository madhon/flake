namespace Flake
{
    using System;

#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class DisposableAction : IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly Action _action;

        public DisposableAction(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

        public void Dispose() => _action();
    }
}