using System.Text.Json.Serialization;

namespace PayTR.PosSelection.Infrastructure.Models.PosSelection.Responses
{
    public class PosSelectionFilters
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
}

