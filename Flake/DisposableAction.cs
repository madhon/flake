namespace Flake
{
    using System;

    public class DisposableAction : IDisposable
    {
        private readonly Action _action;

        public DisposableAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}