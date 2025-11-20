using PayTR.PosSelection.Infrastructure.Services;

namespace PayTR.PosSelection.Infrastructure.Factory
{
    public class USDCalculator: PriceCalculator
    {
        public override string Currency { get; } = "USD";
        
        public override decimal Multiplier { get; set; } = 1.01m;
        
        // USD: cost = max(amount * commission_rate * 1.01, min_fee)
        public override decimal Calculate(decimal amount, decimal commissionRate, decimal minFee)
        { 
            return RoundHalfUp(Math.Max(amount * commissionRate * Multiplier, minFee), 2);
        }
    }
}

