using RepEngine.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace RepEngine.Services;

public class FairScoreService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FairScoreService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private const int CACHE_DURATION_MINUTES = 5;

    public FairScoreService(
        IMemoryCache cache,
        ILogger<FairScoreService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _cache = cache;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<FairScoreResponse> GetScoreAsync(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        var cacheKey = $"fairscore_{walletAddress}";
        if (_cache.TryGetValue<FairScoreResponse>(cacheKey, out var cached))
        {
            _logger.LogInformation("Returning cached FairScore for wallet {Wallet}", walletAddress);
            return cached!;
        }

        var apiKey = _config["FairScale:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
        {
            throw new InvalidOperationException("FairScale API Key is missing. Please set FairScale__ApiKey in your environment variables.");
        }

        var response = await FetchFromApiAsync(walletAddress, apiKey);
        
        // Cache successful response
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        _logger.LogInformation("FairScore {Score} for wallet {Wallet}", response.Fairscore, walletAddress);

        return response;
    }

    private async Task<FairScoreResponse> FetchFromApiAsync(string walletAddress, string apiKey)
    {
        var baseUrl = _config["FairScale:ApiBaseUrl"] ?? "https://api2.fairscale.xyz";
        var client = _httpClientFactory.CreateClient("FairScale");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("fairkey", apiKey);

        var url = $"{baseUrl}/score?wallet={Uri.EscapeDataString(walletAddress)}";
        try 
        {
            var json = await client.GetStringAsync(url);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<FairScaleApiResponse>(json, opts)
                      ?? throw new InvalidOperationException("Empty response from FairScale API");

            return MapApiResponse(walletAddress, raw);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching from FairScale API: {Url}", url);
            throw; // Propagate error so UI shows it
        }
    }

    private static FairScoreResponse MapApiResponse(string wallet, FairScaleApiResponse raw)
    {
        var score = (int)Math.Min(100, Math.Round(raw.Fairscore));
        
        // Default history generation logic removed as API doesn't provide history yet
        // We return an empty history for now or could implement a basic placeholder in frontend
        var history = new List<HistoricalScore>(); 

        return new FairScoreResponse
        {
            WalletAddress = wallet,
            FairscoreBase = raw.FairscoreBase,
            SocialScore = raw.SocialScore,
            Fairscore = raw.Fairscore,
            Tier = raw.Tier,
            LastUpdated = raw.Timestamp ?? DateTime.UtcNow,
            Badges = (raw.Badges ?? new()).Select(b => new FairScoreBadge
            {
                Id = b.Id ?? "",
                Label = b.Label ?? "",
                Description = b.Description ?? "",
                BadgeTier = b.Tier ?? ""
            }).ToList(),
            Features = new FairScoreFeatures
            {
                TxCount = (int)(raw.Features?.TxCount ?? 0),
                WalletAgeDays = (int)(raw.Features?.WalletAgeDays ?? 0),
                ActiveDays = (int)(raw.Features?.ActiveDays ?? 0),
                LstPercentileScore = raw.Features?.LstPercentileScore ?? 0,
                MajorPercentileScore = raw.Features?.MajorPercentileScore ?? 0,
                NativeSolPercentile = raw.Features?.NativeSolPercentile ?? 0,
                StablePercentileScore = raw.Features?.StablePercentileScore ?? 0,
                MedianGapHours = raw.Features?.MedianGapHours ?? 0,
            },
            History = history
        };
    }

    public async Task<List<string>> GetImprovementSuggestionsAsync(string walletAddress)
    {
        try 
        {
            var scoreData = await GetScoreAsync(walletAddress);
            var suggestions = new List<string>();

            if (scoreData.Features.TxCount < 100)
                suggestions.Add("💰 Increase your on-chain transaction activity to boost your score");

            if (scoreData.Features.WalletAgeDays < 90)
                suggestions.Add("⏰ Your account age will naturally improve your score over time");

            if (scoreData.Features.ActiveDays < 30)
                suggestions.Add("🏦 Participate more consistently across multiple days");

            if (scoreData.SocialScore < 20)
                suggestions.Add("🌐 Connect your social accounts to unlock your social reputation score");

            if (scoreData.Badges.Count == 0)
                suggestions.Add("🏆 Earn badges by participating in DeFi, governance, and long-term holding");

            if (suggestions.Count == 0)
                suggestions.Add("🌟 You're doing great! Keep maintaining your high on-chain reputation");

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate suggestions for wallet {Wallet}", walletAddress);
            return new List<string> { "Unable to generate suggestions at this time." };
        }
    }

    // ── Internal DTOs ────────────────────────────────────────────────────────
    private class FairScaleApiResponse
    {
        [JsonPropertyName("wallet")] public string? Wallet { get; set; }
        [JsonPropertyName("fairscore_base")] public double FairscoreBase { get; set; }
        [JsonPropertyName("social_score")] public double SocialScore { get; set; }
        [JsonPropertyName("fairscore")] public double Fairscore { get; set; }
        [JsonPropertyName("tier")] public string Tier { get; set; } = string.Empty;
        [JsonPropertyName("badges")] public List<ApiBadge>? Badges { get; set; }
        [JsonPropertyName("features")] public ApiFeatures? Features { get; set; }
        [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; set; }
    }

    private class ApiBadge
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("tier")] public string? Tier { get; set; }
    }

    private class ApiFeatures
    {
        [JsonPropertyName("lst_percentile_score")] public double LstPercentileScore { get; set; }
        [JsonPropertyName("major_percentile_score")] public double MajorPercentileScore { get; set; }
        [JsonPropertyName("native_sol_percentile")] public double NativeSolPercentile { get; set; }
        [JsonPropertyName("stable_percentile_score")] public double StablePercentileScore { get; set; }
        [JsonPropertyName("tx_count")] public double TxCount { get; set; }
        [JsonPropertyName("active_days")] public double ActiveDays { get; set; }
        [JsonPropertyName("median_gap_hours")] public double MedianGapHours { get; set; }
        [JsonPropertyName("wallet_age_days")] public double WalletAgeDays { get; set; }
    }
}
