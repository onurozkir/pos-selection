using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PayTR.PosSelection.Infrastructure.Factory.Interfaces;
using PayTR.PosSelection.Infrastructure.Interfaces.PosRatios;
using PayTR.PosSelection.Infrastructure.Interfaces.PosSelection;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;
using PayTR.PosSelection.Infrastructure.Models.PosRatios; 
using PayTR.PosSelection.Infrastructure.Models.PosSelection.Responses;
using StackExchange.Redis;

namespace PayTR.PosSelection.Infrastructure.Services
{
    public class PosSelection : IPosSelection
    {
        private readonly IRedisClient _redis; 
        private readonly IPriceCalculatorFactory _priceCalculatorFactory;
        private readonly IConfiguration _configuration;
        private readonly IPosRatios _posRatios;
        
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly string CacheDisabledKey = "pos-selection:cache:disabled";
        
        public PosSelection(
            IRedisClient redis,  
            IPriceCalculatorFactory priceCalculatorFactory,
            IConfiguration configuration,
            IPosRatios posRatios
            )
        {
            _redis = redis; 
            _priceCalculatorFactory = priceCalculatorFactory;
            _configuration = configuration;
            _posRatios = posRatios;
        }
        
        public async Task<Models.PosSelection.Responses.PosSelection?> SelectBestPosAsync(Models.PosSelection.Requests.PosSelection request, CancellationToken cancellationToken)
        {
       
            var version = await _redis.Get("pos-ratios:current"); 
            var snapshotKey = $"pos-ratios:{version}";
            
            // check cache with filter
            var cacheKey = BuildCacheKey(version, request);
            var cachedJson = await _redis.Get(cacheKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<Models.PosSelection.Responses.PosSelection>(cachedJson!, _jsonSerializerOptions);
                if (cached != null)
                {
                    // 5 dk içinde daha önce aynı parametreler ile istek atıldı
                    // cevap cacheden verildi
                    return cached;
                }
            }
            
            // call redis
            var snapshotJson = await _redis.Get(snapshotKey);
            if (string.IsNullOrEmpty(snapshotJson))
            {
                // check db
                snapshotJson = await _posRatios.GetLastVersion(cancellationToken);
            }

            if (string.IsNullOrEmpty(snapshotJson))
            {
                throw new NotFoundException("No POS information could be found.");
            }
            
            // redis'te yok ama db'de var ise db'deki değeri redis'e yazmak gerekliydi
            
            // get ratios
            // çok fazla serialize ediyoruz RedisJSON kurulup direk json kaydedilebilir
            var ratios = JsonSerializer.Deserialize<List<PosRatio>>(snapshotJson!, _jsonSerializerOptions)
                         ?? new List<PosRatio>();
            
            if (ratios.Count == 0)
            {
                throw new NotFoundException("No POS information could be found.");
            } 
            
            // filter request
            // RedisJSON kurmanın diğer avantajı filtrelemeyi redis'e yıkabiliridk ama bu sefer db'de de satır satır kaydetmemiz lazımdı
            var filtered = ratios
                .Where(r => r.Installment == request.Installment)
                .Where(r => string.Equals(r.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if(!string.IsNullOrWhiteSpace(request.CardType))    
                filtered =  filtered.Where(r => r.CardType == request.CardType)
                    .ToList();
            
            if(!string.IsNullOrWhiteSpace(request.CardBrand))    
                filtered =  filtered.Where(r => r.CardBrand == request.CardBrand)
                    .ToList();


            if (filtered.Count == 0)
            {
                throw new NotFoundException("No POS information was found. Please try again with different filters.");
            }
            
            // call factory
            var calculator = _priceCalculatorFactory.GetCalculator(request.Currency);
            
            // calculate payable_total, price
            var candidates = filtered
                .Select(r =>
                {
                    // buradan okumak maliyetli bunu bootstrap'da inject etmek gerekebilirdi
                    calculator.Multiplier = decimal.Parse(_configuration[$"PosSelectionMultiplier:{calculator.Currency}"], CultureInfo.InvariantCulture);
                    var price = calculator.Calculate(request.Amount, r.CommissionRate, r.MinFee);
                    var payableTotal = Math.Round(request.Amount + price, 2, MidpointRounding.AwayFromZero);

                    return new PosCandidate(r, price, payableTotal);
                })
                .ToList();
            
            // sorting
            var best = candidates
                .OrderBy(c => c.Price)
                .ThenByDescending(c => c.Ratio.Priority)
                .ThenBy(c => c.Ratio.CommissionRate)
                .ThenBy(c => c.Ratio.PosName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            // gereksiz kaldı First() yapılsa belki
            if (best == null)
            {
                throw new NotFoundException("No POS information could be found.");
            }
            
            var response = new Models.PosSelection.Responses.PosSelection
            {
                Filters = new PosSelectionFilters
                {
                    Amount = request.Amount,
                    Installment = request.Installment,
                    Currency = request.Currency,
                    CardType = request.CardType,
                    CardBrand = request.CardBrand
                },
                OverallMin = new PosSelectionResult
                {
                    PosName = best.Ratio.PosName,
                    CardType = best.Ratio.CardType,
                    CardBrand = best.Ratio.CardBrand,
                    Installment = best.Ratio.Installment,
                    Currency = best.Ratio.Currency,
                    CommissionRate = best.Ratio.CommissionRate,
                    Price = best.Price,
                    PayableTotal = best.PayableTotal,
                    Priority = best.Ratio.Priority
                }
            };
            
            // cache write flag var mı?
            var flag = await _redis.Get(CacheDisabledKey);
            
            // eğer cache write flag yok ise yaz
            if (string.IsNullOrWhiteSpace(flag))
            {
                // response'su 5 dk cache yaz aynı parametreler ile gelirlerse 
                // cacheden ver
                var responseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);
                await _redis.Set(
                    cacheKey,
                    responseJson,
                    expiration: TimeSpan.FromMinutes(5),
                    when: When.Always); 
            }
            
            return response; 
        }
        
        
        private static string BuildCacheKey(string version, Models.PosSelection.Requests.PosSelection request)
        {
            // cache hit mi? cpu cost mu?
            var normalizedAmount = request.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            
            var currency = request.Currency.ToUpperInvariant();

            var cardType = string.IsNullOrWhiteSpace(request.CardType)
                ? "None"
                : request.CardType.Trim().ToUpperInvariant();
            
            var cardBrand = string.IsNullOrWhiteSpace(request.CardBrand)
                ? "None"
                : request.CardBrand.Trim().ToUpperInvariant(); 
            
            // pos-selection:20251120:362.22:6:TRY:CREDIT:BONUS
            // pos-selection:20251120:362.22:6:TRY:None:None
            return $"pos-selection:{version}:{normalizedAmount}:{request.Installment.ToString()}:{currency}:{cardType}:{cardBrand}";
        }
    }
}

