namespace Flake
{
    using System;

    public class InvalidSystemClockException : Exception
    {
        public InvalidSystemClockException(string message) : base(message)
        {
        }
    }
}