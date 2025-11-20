using PayTR.PosSelection.Infrastructure.Factory.Interfaces;
using PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator;

namespace PayTR.PosSelection.Infrastructure.Factory
{
    public class PriceCalculatorFactory: IPriceCalculatorFactory
    {
        private readonly Dictionary<string, IPriceCalculator> _calculators;
        private readonly IPriceCalculator _defaultCalculator;

        public PriceCalculatorFactory(IEnumerable<IPriceCalculator> calculators)
        {
   
            _calculators = calculators
                .ToDictionary(
                    c => c.Currency,
                    c => c,
                    StringComparer.OrdinalIgnoreCase);
            

            _defaultCalculator = calculators.First(c => c.Currency == "TRY");
        }

        public IPriceCalculator GetCalculator(string currency)
        {
            if (_calculators.TryGetValue(currency, out var calculator))
            {
                return calculator;
            }

            return _defaultCalculator;
        }
    }
}

