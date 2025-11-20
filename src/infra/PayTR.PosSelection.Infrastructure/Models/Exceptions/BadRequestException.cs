using System.Net;

namespace PayTR.PosSelection.Infrastructure.Models.Exceptions
{
    public class BadRequestException: Exception
    {
        public int StatusCode { get; set; }

        public BadRequestException()
        {
            StatusCode = (int)HttpStatusCode.BadRequest;
        }
    
        public BadRequestException(string message) : base(message)
        {
            StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
}

