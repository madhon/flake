namespace Flake
{
  using System;

  [Serializable]
  public class InvalidSystemClockException : Exception
  {
    public InvalidSystemClockException(string message) : base(message)
    {
    }
  }
}