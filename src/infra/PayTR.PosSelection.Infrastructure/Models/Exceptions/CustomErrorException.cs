using System.Net;

namespace PayTR.PosSelection.Infrastructure.Models.Exceptions
{
    public class CustomErrorException: Exception
    {
        public int StatusCode { get; set; }
        public string Type { get; set; }

        public CustomErrorException()
        {
            StatusCode = (int)HttpStatusCode.InternalServerError; 
        }
        
        public CustomErrorException(string message) : base(message)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError; 
        }
    
        public CustomErrorException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode; 
        }
        
        public CustomErrorException(string message, Exception exception) : base(message, exception)
        {
            StatusCode =(int)HttpStatusCode.InternalServerError;
            Type = exception.GetType().Name;
        }
    
        public CustomErrorException(string message, int statusCode, Exception exception) : base(message, exception)
        {
            StatusCode = statusCode; 
            Type = exception.GetType().Name;
        }
    }
}

