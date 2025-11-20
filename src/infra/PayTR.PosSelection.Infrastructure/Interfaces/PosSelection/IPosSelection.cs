using PayTR.PosSelection.Infrastructure.Models.PosSelection.Requests;
using PayTR.PosSelection.Infrastructure.Models.PosSelection.Responses;

namespace PayTR.PosSelection.Infrastructure.Interfaces.PosSelection
{
    public interface IPosSelection
    {
        Task<Models.PosSelection.Responses.PosSelection?> SelectBestPosAsync(Models.PosSelection.Requests.PosSelection request, CancellationToken cancellationToken);
    }
}

