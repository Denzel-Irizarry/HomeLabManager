
namespace HomeLabManager.API.ExceptionsAPI
{
    public class SerialNumberMissingException : Exception
    {
        public SerialNumberMissingException() : base("Serial number is missing from the request.")
        {
        }

        //this need to be implemented when i begin with logging 
        public SerialNumberMissingException(string message) : base(message)
        {
        }
        public SerialNumberMissingException(string message, Exception innerException) : base(message, innerException)
        {
            
        }


    }
}