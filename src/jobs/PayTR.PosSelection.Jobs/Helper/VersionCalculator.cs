using Quartz;

namespace PayTR.PosSelection.Jobs.Helper
{
    public abstract class VersionCalculator
    {
        public static int CalculateVersion(IJobExecutionContext context)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        
            DateTimeOffset jobStartDate = TimeZoneInfo.ConvertTimeFromUtc(context.FireTimeUtc.UtcDateTime, tz);;
        
            DateTimeOffset jobEndDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var diff = jobEndDate - jobStartDate;
        
            DateTime resultDateInt = DateTime.Now.AddDays(1);

            if (diff >= TimeSpan.FromMinutes(1))
            {
                resultDateInt = DateTime.Now;
            } 

            return int.Parse(resultDateInt.ToString("yyyyMMdd"));
        }
    }
}

