using System.Text.Json.Serialization;

namespace PayTR.PosSelection.Jobs.Services.Model
{
    public class RatioDTO
    {
        [JsonPropertyName("pos_name")]
        public string PosName { get; set; } 
        [JsonPropertyName("card_type")]
        public string CardType { get; set; } 
        [JsonPropertyName("card_brand")]
        public string CardBrand { get; set; } 
        [JsonPropertyName("installment")]
        public int Installment { get; set; } 
        [JsonPropertyName("currency")]
        public string Currency { get; set; } 
        [JsonPropertyName("commission_rate")]
        public decimal CommissionRate { get; set; } 
        [JsonPropertyName("min_fee")]
        public decimal MinFee { get; set; } 
        [JsonPropertyName("priority")]
        public int Priority { get; set; } 
    }
}

