namespace PayTR.PosSelection.Infrastructure.Interfaces.PosRatios
{
    public interface IPosRatios
    {
        Task<bool> InsertVersion(
            int version,
            string ratiosJson,
            DateTimeOffset posReceiveFinishDate,
            CancellationToken cancellationToken);

        Task<string?> GetLastVersion(CancellationToken cancellationToken);
    }
}

