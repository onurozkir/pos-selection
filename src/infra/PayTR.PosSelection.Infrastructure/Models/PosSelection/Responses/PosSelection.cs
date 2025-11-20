using System.Text.Json.Serialization;

namespace PayTR.PosSelection.Infrastructure.Models.PosSelection.Responses
{
    public class PosSelection
    {
        [JsonPropertyName("filters")]
        public PosSelectionFilters Filters { get; set; }

        [JsonPropertyName("overall_min")]
        public PosSelectionResult OverallMin { get; set; }
    }
}

