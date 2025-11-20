using System.Net;

namespace PayTR.PosSelection.Infrastructure.Models.Exceptions
{
    public class NotFoundException: Exception
    {
        public int StatusCode { get; set; }

        public NotFoundException()
        {
            StatusCode = (int)HttpStatusCode.NotFound;
        }
    
        public NotFoundException(string message) : base(message)
        {
            StatusCode = (int)HttpStatusCode.NotFound;
        }
    }
}