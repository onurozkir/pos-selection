using PayTR.PosSelection.Infrastructure.Services;

namespace PayTR.PosSelection.Infrastructure.Factory
{
    public class TRYCalculator : PriceCalculator
    {
        public override string Currency => "TRY";
        
        public override decimal Multiplier { get; set; } = 1.00m;
        
        // TRY: cost = max(amount * commission_rate * 1, min_fee)
        public override decimal Calculate(decimal amount, decimal commissionRate, decimal minFee)
        { 
            return RoundHalfUp(Math.Max(amount * commissionRate * Multiplier, minFee), 2);
        }
    }
}

