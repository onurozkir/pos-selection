using System.Text.Json;
using PayTR.PosSelection.Infrastructure.Interfaces.RationApiClient;
using PayTR.PosSelection.Infrastructure.Interfaces.PosRatios;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Jobs.Helper;
using PayTR.PosSelection.Jobs.Services.Model;
using Quartz;

namespace PayTR.PosSelection.Jobs.Jobs
{
    public class RatiosJobs : IJob
    {
        private readonly ILogger<RatiosJobs> _logger;
        private readonly IRatiosApiClient _apiClient;
        private readonly IPosRatios _repository;
        private readonly IRedisClient _redis;

        public RatiosJobs(
            ILogger<RatiosJobs> logger,
            IRatiosApiClient apiClient,
            IPosRatios repository,
            IRedisClient redis)
        {
            _logger = logger;
            _apiClient = apiClient;
            _repository = repository;
            _redis = redis;
        }
         
        public async Task Execute(IJobExecutionContext context)
        { 
            
            _logger.LogInformation("RatiosJob started at {time}", DateTimeOffset.Now);
            try
            {
                var ct = context.CancellationToken; 
                
                // fetch
                List<RatioDTO> ratios = await _apiClient.FetchRatios(ct);
                
                if (ratios.Count <= 0)
                {
                    _logger.LogError("Ratios API is Empty abort job.");
                    return;
                }
                
                var ratiosJson = JsonSerializer.Serialize(ratios);
                 
                // api 1 dakikadan uzun sürmüş olabilir
                // 23:59:01'de biterse version olarak Datetime.Now + 1 days almamız
                // 00:00:10'da biterse version olarak Datetime.Now almamız gerek
                var version = VersionCalculator.CalculateVersion(context);
                
                var versionKey = $"pos-ratios:{version}";
                
                // yeni bir pointer olarak kaydet
                await _redis.Set(versionKey, ratiosJson);
                
                // api hala istekte bulunuyor o yüzden resultları cacheye yazmayı durdur
                await _redis.Set("pos-selection:cache-disabled", "1");
                
                // apinin set ettiği tüm resultları cache'den sil
                await _redis.DeleteAllKey("pos-selection:*");
                
                // current=last-version yap
                await _redis.Set("pos-ratios:current", version.ToString());
                
                // redis değişti ve artık 23:59:00 - DateTimeOffset.UtcNow arasındak kimler eski dataları kullandı transaction tablosundan görebiliriz.
                var posReceiveFinishDate = DateTimeOffset.UtcNow;
                
                // write DB
                var dbResult = await _repository.InsertVersion(version, ratiosJson, posReceiveFinishDate, ct);
                
                if(!dbResult)  
                    _logger.LogInformation("{Version} versions of data db are also available.", version);
                
                // cache write flag kaldır
                await _redis.Delete("pos-selection:cache-disabled");
                
                _logger.LogInformation("Fetched {Count} POS ratios from external API.", ratios.Count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred while fetching POS ratios.");
            }
            _logger.LogInformation("PosRatiosSyncJob finished at {time}", DateTimeOffset.Now);
        }
    }
}

