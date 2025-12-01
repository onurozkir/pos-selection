namespace PayTR.PosSelection.Infrastructure.Models.RatiosJob
{
    public class RatiosJobOptions
    {
        public static string SectionName { get; set; } = "RatiosJob";
        public static string Cron { get; set; } = "1 59 23 * * ?";
        public string RatiosApiUrl { get; set; } = string.Empty;
        public int HttpTimeoutSeconds { get; set; } = 5;
    }
}

