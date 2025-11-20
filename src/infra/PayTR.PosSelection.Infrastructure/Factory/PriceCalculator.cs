using PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator;

namespace PayTR.PosSelection.Infrastructure.Services
{
    public abstract class PriceCalculator: IPriceCalculator
    {
        public abstract string Currency { get; }
        
        public abstract decimal Multiplier { get; set; }
        
        public abstract decimal Calculate(decimal amount, decimal commissionRate, decimal minFee);
        
        protected static decimal RoundHalfUp(decimal value, int decimals)
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}

