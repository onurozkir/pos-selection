using PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator;

namespace PayTR.PosSelection.Infrastructure.Factory.Interfaces
{
    public interface IPriceCalculatorFactory
    {
        IPriceCalculator GetCalculator(string currency);
    }
}

