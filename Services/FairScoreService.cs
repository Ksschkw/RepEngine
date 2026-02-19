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

        FairScoreResponse response;

        var apiKey = _config["FairScale:ApiKey"];
        var useMock = _config.GetValue<bool>("FairScale:UseMockData", true);

        if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR_API_KEY_HERE" && !useMock)
        {
            response = await FetchFromApiAsync(walletAddress, apiKey);
        }
        else
        {
            _logger.LogInformation("Using mock FairScore data for wallet {Wallet}", walletAddress);
            response = GenerateMockResponse(walletAddress);
        }

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        _logger.LogInformation("FairScore {Score} for wallet {Wallet}", response.Fairscore, walletAddress);

        return response;
    }

    // ── Real API call ──────────────────────────────────────────────────────

    private async Task<FairScoreResponse> FetchFromApiAsync(string walletAddress, string apiKey)
    {
        var baseUrl = _config["FairScale:ApiBaseUrl"] ?? "https://api.fairscale.xyz";
        var client = _httpClientFactory.CreateClient("FairScale");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("fairkey", apiKey);

        var url = $"{baseUrl}/score?wallet={Uri.EscapeDataString(walletAddress)}";
        var json = await client.GetStringAsync(url);

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw = JsonSerializer.Deserialize<FairScaleApiResponse>(json, opts)
                  ?? throw new InvalidOperationException("Empty response from FairScale API");

        return MapApiResponse(walletAddress, raw);
    }

    private static FairScoreResponse MapApiResponse(string wallet, FairScaleApiResponse raw)
    {
        var score = (int)Math.Min(100, Math.Round(raw.Fairscore));
        var tier = ReputationTier.GetTierByName(raw.Tier);

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
            History = GenerateHistoryFromScore(score)
        };
    }

    // ── Mock data fallback ─────────────────────────────────────────────────

    private FairScoreResponse GenerateMockResponse(string walletAddress)
    {
        var score = GenerateDeterministicScore(walletAddress);
        var seed = walletAddress.GetHashCode();
        var rand = new Random(seed);

        // Simulate API tier names
        string tierName = score switch
        {
            >= 80 => "platinum",
            >= 60 => "gold",
            >= 40 => "silver",
            >= 20 => "bronze",
            _ => "bronze"
        };

        // Simulated sub-scores
        double fairscoreBase = score * 0.65;
        double socialScore = score * 0.35;

        return new FairScoreResponse
        {
            WalletAddress = walletAddress,
            FairscoreBase = Math.Round(fairscoreBase, 1),
            SocialScore = Math.Round(socialScore, 1),
            Fairscore = score,
            Tier = tierName,
            LastUpdated = DateTime.UtcNow,
            Badges = GenerateMockBadges(score, rand),
            Features = new FairScoreFeatures
            {
                TxCount = rand.Next(50, 5000),
                WalletAgeDays = rand.Next(30, 1200),
                ActiveDays = rand.Next(10, 500),
                LstPercentileScore = Math.Round(rand.NextDouble(), 2),
                MajorPercentileScore = Math.Round(rand.NextDouble(), 2),
                NativeSolPercentile = Math.Round(rand.NextDouble(), 2),
                StablePercentileScore = Math.Round(rand.NextDouble(), 2),
                MedianGapHours = Math.Round(rand.NextDouble() * 48, 1)
            },
            History = GenerateHistoryFromScore(score)
        };
    }

    private int GenerateDeterministicScore(string walletAddress)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(walletAddress));
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        return (int)(hashValue % 101); // 0–100
    }

    private static List<FairScoreBadge> GenerateMockBadges(int score, Random rand)
    {
        var allBadges = new List<(string id, string label, string desc, string tier)>
        {
            ("diamond_hands", "Diamond Hands", "Long-term holder with conviction", "platinum"),
            ("defi_native", "DeFi Native", "Active across multiple DeFi protocols", "gold"),
            ("early_adopter", "Early Adopter", "Joined the ecosystem early", "silver"),
            ("active_voter", "Active Voter", "Participates in on-chain governance", "gold"),
            ("consistent_trader", "Consistent Trader", "Regular on-chain activity", "bronze")
        };

        int count = score >= 80 ? 3 : score >= 60 ? 2 : score >= 40 ? 1 : 0;
        return allBadges
            .OrderBy(_ => rand.Next())
            .Take(count)
            .Select(b => new FairScoreBadge { Id = b.id, Label = b.label, Description = b.desc, BadgeTier = b.tier })
            .ToList();
    }

    private static List<HistoricalScore> GenerateHistoryFromScore(int currentScore)
    {
        var history = new List<HistoricalScore>();
        var score = Math.Max(0, currentScore - Random.Shared.Next(10, 30));
        for (int i = 0; i < 6; i++)
        {
            history.Add(new HistoricalScore
            {
                Date = DateTime.UtcNow.AddMonths(i - 5),
                Score = Math.Min(100, score + Random.Shared.Next(0, 10))
            });
            score = history.Last().Score;
        }
        return history;
    }

    public async Task<List<string>> GetImprovementSuggestionsAsync(string walletAddress)
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

    // ── Internal DTOs for deserialising the FairScale API ─────────────────────

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
