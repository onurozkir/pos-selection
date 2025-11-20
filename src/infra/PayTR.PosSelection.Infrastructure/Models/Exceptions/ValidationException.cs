using System.Net;

namespace PayTR.PosSelection.Infrastructure.Models.Exceptions
{
    public class ValidationException: Exception
    {
        public List<(int, string)> Errors { get; }
        public int StatusCode { get; set; } = (int)HttpStatusCode.BadRequest;
        
        public ValidationException(List<(int, string)> ValidationMessage): base()
        {
            Errors = ValidationMessage;
        }
    }
}

