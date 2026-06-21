using System;
using System.IO;

namespace KMS.Sdk
{
  public class KMSException : Exception
  {
    public KMSException(string message) : base(message)
    {
    }

    public KMSException(IOException cause) : base(cause?.Message, cause)
    {
    }

    // Optional: Add additional constructors following standard .NET exception patterns
    public KMSException() : base()
    {
    }

    public KMSException(string message, Exception innerException) : base(message, innerException)
    {
    }
  }
}