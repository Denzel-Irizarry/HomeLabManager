using System;

namespace HomeLabManager.API.ExceptionsAPI
{
    public class DuplicateSerialNumberException : Exception
    {
        public DuplicateSerialNumberException()
        {
        }

        public DuplicateSerialNumberException(string message)
            : base(message)
        {
        }

        public DuplicateSerialNumberException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
