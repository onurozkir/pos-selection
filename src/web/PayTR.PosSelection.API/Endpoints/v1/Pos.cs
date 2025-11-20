using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayTR.PosSelection.Infrastructure.Interfaces.PosSelection;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;
using PayTR.PosSelection.Infrastructure.Models.PosSelection.Requests;

namespace PayTR.PosSelection.API.Endpoints.v1
{
    public class Pos : BaseEndpoints
    {
        public static async Task<IResult> PosSelection([FromServices] IHttpContextAccessor accessor,
            [FromServices] IPosSelection service,
            [FromBody] Infrastructure.Models.PosSelection.Requests.PosSelection posSelection,
            IValidator<Infrastructure.Models.PosSelection.Requests.PosSelection> posSelectionRequestValidator,
            CancellationToken cancellationToken
        )
        {
            if(accessor.HttpContext is { RequestAborted.IsCancellationRequested: true})
                throw new BadRequestException("Task was called before the request.");
            
            // TODO:
            // - validation
            // - call service
            // - return response 
            
            await posSelectionRequestValidator.ValidateAsync(posSelection, cancellationToken);
            
            var result = await service.SelectBestPosAsync(posSelection, cancellationToken); 

            return Results.Ok(result);
        }
    }
}

