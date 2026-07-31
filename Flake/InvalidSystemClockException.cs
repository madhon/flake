namespace Flake;

using System;

public sealed class InvalidSystemClockException : Exception
{
    public InvalidSystemClockException(string message) : base(message)
    {
    }

    public InvalidSystemClockException() : base()
    {
    }

    public InvalidSystemClockException(string message, Exception innerException) : base(message, innerException)
    {
    }
}