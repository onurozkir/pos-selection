using System.Text.Json.Serialization;
using PayTR.PosSelection.Infrastructure.Models.PosRatios;

namespace PayTR.PosSelection.Infrastructure.Models.PosSelection.Responses
{
    public class PosSelectionResult : PosRatio
    { 
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
        [JsonPropertyName("payable_total")]
        public decimal PayableTotal { get; set; } 
    }
    
    public sealed record PosCandidate(
        PosRatio Ratio,
        decimal Price,
        decimal PayableTotal);
}

