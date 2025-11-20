using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Results;
using ValidationException = PayTR.PosSelection.Infrastructure.Models.Exceptions.ValidationException;

namespace PayTR.PosSelection.Infrastructure.Models.PosSelection.Requests
{
    public class PosSelection
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }
        [JsonPropertyName("installment")]
        public int Installment { get; init; }
        [JsonPropertyName("currency")]
        public string Currency { get; init; }
        [JsonPropertyName("card_type")]
        public string? CardType { get; init; }
        [JsonPropertyName("card_brand")]
        public string? CardBrand { get; init; }
    }

    public class PosSelectionRequestValidator : AbstractValidator<PosSelection>
    {
        public PosSelectionRequestValidator()
        {
            RuleFor(x => x.Amount)
                .NotNull().WithMessage("Amount is required")
                .NotEmpty().WithMessage("Amount is required")
                .GreaterThan(0).WithMessage("Amount must be greater than zero");
            RuleFor(x => x.Installment)
                .NotNull().WithMessage("Installment is required")
                .NotEmpty().WithMessage("Installment is required")
                .GreaterThan(0).WithMessage("Installment must be greater than zero");
            RuleFor(x => x.Currency)
                .NotNull().WithMessage("Currency is required")
                .NotEmpty().WithMessage("Currency is required")
                .Must(c => c.Equals("TRY", StringComparison.OrdinalIgnoreCase)
                           || c.Equals("USD", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Currency can only be 'TRY' and 'USD'");
            RuleFor(x => x.CardType)
                .Must(v => v == "credit" || v == "debit")
                .When(x => !string.IsNullOrWhiteSpace(x.CardType))
                .WithMessage("CardType can be 'credit' or 'debit'.");

        }

        public override async Task<ValidationResult> ValidateAsync(ValidationContext<PosSelection> context, CancellationToken cancellation = new CancellationToken())
        {
            var validateionResult = await base.ValidateAsync(context, cancellation);
            if (!validateionResult.IsValid)
            {
                throw new ValidationException(
                    validateionResult.Errors
                        .Select(
                            error => ((int)HttpStatusCode.BadRequest, error.ErrorMessage)).ToList());
            }
            return validateionResult;
        }
    }
}

