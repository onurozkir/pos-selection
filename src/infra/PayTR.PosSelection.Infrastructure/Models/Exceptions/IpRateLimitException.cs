using System.Net;

namespace PayTR.PosSelection.Infrastructure.Models.Exceptions
{
    public class IpRateLimitException : Exception
    {
        public int StatusCode { get; set; }

        public IpRateLimitException()
        {
            StatusCode = (int)HttpStatusCode.TooManyRequests;
        }
    
        public IpRateLimitException(string message) : base(message)
        {
            StatusCode = (int)HttpStatusCode.TooManyRequests;
        }
    }
}

