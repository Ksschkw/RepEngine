using RepEngine.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

public class FairScoreService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FairScoreService> _logger;
    private const int CACHE_DURATION_MINUTES = 5;

    public FairScoreService(IMemoryCache cache, ILogger<FairScoreService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<FairScoreResponse> GetScoreAsync(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));
        }

        // Check cache first
        var cacheKey = $"fairscore_{walletAddress}";
        if (_cache.TryGetValue<FairScoreResponse>(cacheKey, out var cachedScore))
        {
            _logger.LogInformation("Returning cached FairScore for wallet {Wallet}", walletAddress);
            return cachedScore!;
        }

        // Simulate API call latency
        await Task.Delay(Random.Shared.Next(100, 300));

        // Generate deterministic score based on wallet address
        var score = GenerateDeterministicScore(walletAddress);
        var breakdown = GenerateScoreBreakdown(walletAddress);
        var tier = ReputationTier.GetTierByScore(score);

        var response = new FairScoreResponse
        {
            WalletAddress = walletAddress,
            Score = score,
            Tier = tier.Name,
            LastUpdated = DateTime.UtcNow,
            Breakdown = breakdown,
            History = GenerateHistory(score)
        };

        // Cache the response
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        _logger.LogInformation("Generated FairScore {Score} for wallet {Wallet}", score, walletAddress);

        return response;
    }

    private int GenerateDeterministicScore(string walletAddress)
    {
        // Use wallet address hash to generate consistent score (0-100)
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(walletAddress));
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        return (int)(hashValue % 101); // 0-100
    }

    private ScoreBreakdown GenerateScoreBreakdown(string walletAddress)
    {
        var baseScore = GenerateDeterministicScore(walletAddress);
        var seed = walletAddress.GetHashCode();
        var random = new Random(seed);

        return new ScoreBreakdown
        {
            TransactionVolume = random.Next(0, 25),
            AccountAge = random.Next(0, 20),
            DeFiActivity = random.Next(0, 20),
            GovernanceParticipation = random.Next(0, 20),
            SocialReputation = random.Next(0, 15)
        };
    }

    private List<HistoricalScore> GenerateHistory(int currentScore)
    {
        var history = new List<HistoricalScore>();
        var date = DateTime.UtcNow.AddMonths(-6);
        var score = Math.Max(0, currentScore - Random.Shared.Next(10, 30));

        for (int i = 0; i < 6; i++)
        {
            history.Add(new HistoricalScore
            {
                Date = date.AddMonths(i),
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

        if (scoreData.Breakdown.TransactionVolume < 15)
            suggestions.Add("💰 Increase your transaction volume to boost your score");

        if (scoreData.Breakdown.AccountAge < 10)
            suggestions.Add("⏰ Your account age will naturally improve over time");

        if (scoreData.Breakdown.DeFiActivity < 15)
            suggestions.Add("🏦 Participate in DeFi protocols to increase your activity score");

        if (scoreData.Breakdown.GovernanceParticipation < 15)
            suggestions.Add("🗳️ Vote on DAO proposals to improve governance participation");

        if (scoreData.Breakdown.SocialReputation < 10)
            suggestions.Add("🌐 Build your social reputation through community engagement");

        if (suggestions.Count == 0)
            suggestions.Add("🌟 You're doing great! Keep maintaining your high reputation");

        return suggestions;
    }
}
