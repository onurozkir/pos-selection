using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayTR.PosSelection.Infrastructure.Interfaces.RationApiClient;
using PayTR.PosSelection.Infrastructure.Models.RatiosJob;
using PayTR.PosSelection.Jobs.Services.Model;
using Polly;

namespace PayTR.PosSelection.Infrastructure.Services;

public class RatiosApiClient : IRatiosApiClient
{
    private readonly IHttpClientFactory _httpClient;
    private readonly RatiosJobOptions _options;
    private readonly ILogger<RatiosApiClient> _logger;

    public RatiosApiClient(
        IHttpClientFactory httpClient,
        IOptions<RatiosJobOptions> options,
        ILogger<RatiosApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<RatioDTO>> FetchRatios(CancellationToken cancellationToken)
    {
        
        const int maxAttempts = 4;

        var policy = Policy<List<RatioDTO>>
            .Handle<Exception>()  
            .OrResult(r => r == null || r.Count == 0)
            .WaitAndRetryAsync(
                maxAttempts - 1, // 1 normal + 3 retry = 4 deneme
                _ => TimeSpan.FromSeconds(1),
                (outcome, delay, attempt, _) => { _logger.LogError($"Retry Error {outcome.Exception.Message}"); });
        
        try
        {
            if (string.IsNullOrWhiteSpace(_options.RatiosApiUrl))
                throw new InvalidOperationException("RatiosJobOptions.RatiosApiUrl is not configured.");

            return await policy.ExecuteAsync(async ct =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _options.RatiosApiUrl)
                {
                    Headers =
                    {
                        { "Accept", "application/json" }
                    }
                };
                using var client = _httpClient.CreateClient("RatiosClient");
                client.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("gzip, deflate, br");

                var response = await client.SendAsync(request, ct);
               
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);

                var ratios = await JsonSerializer.DeserializeAsync<List<RatioDTO>>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, ct);

                return ratios ?? new List<RatioDTO>();
            }, cancellationToken);
 
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}