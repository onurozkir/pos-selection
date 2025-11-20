using PayTR.PosSelection.Infrastructure.Models.PosRatios;

namespace PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator
{
    public interface IPriceCalculator
    {
        string Currency { get; }
        decimal Multiplier { get; set; }
        
        decimal Calculate(decimal amount, decimal commissionRate, decimal minFee);
    }
}

