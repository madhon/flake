namespace Flake
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidSystemClockException : Exception
    {
        public InvalidSystemClockException(string message) : base(message)
        {
        }

        protected InvalidSystemClockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}