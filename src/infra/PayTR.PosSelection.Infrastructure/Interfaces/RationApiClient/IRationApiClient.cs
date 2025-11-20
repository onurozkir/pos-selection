using PayTR.PosSelection.Jobs.Services.Model;

namespace PayTR.PosSelection.Infrastructure.Interfaces.RationApiClient
{
    public interface IRatiosApiClient
    {
        Task<List<RatioDTO>> FetchRatios(CancellationToken ct);
    }
}

