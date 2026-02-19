using RepEngine.Models;

namespace RepEngine.Services;

public class ReputationService
{
    private readonly FairScoreService _fairScoreService;
    private readonly ILogger<ReputationService> _logger;

    public ReputationService(FairScoreService fairScoreService, ILogger<ReputationService> logger)
    {
        _fairScoreService = fairScoreService;
        _logger = logger;
    }

    public async Task<ReputationTier> GetUserTierAsync(string walletAddress)
    {
        var scoreData = await _fairScoreService.GetScoreAsync(walletAddress);
        // Prefer the API's tier string if available; fall back to score-based lookup
        if (!string.IsNullOrWhiteSpace(scoreData.Tier))
            return ReputationTier.GetTierByName(scoreData.Tier);

        return ReputationTier.GetTierByScore(scoreData.Score);
    }

    public async Task<bool> CanAccessFeatureAsync(string walletAddress, TierLevel requiredTier)
    {
        var userTier = await GetUserTierAsync(walletAddress);
        return userTier.Level >= requiredTier;
    }

    public async Task<decimal> CalculateVotingPowerAsync(string walletAddress)
    {
        var tier = await GetUserTierAsync(walletAddress);
        var scoreData = await _fairScoreService.GetScoreAsync(walletAddress);
        return scoreData.Score * tier.VotingPowerMultiplier;
    }

    public async Task<decimal> CalculateJobFeeAsync(decimal baseAmount, string walletAddress)
    {
        var tier = await GetUserTierAsync(walletAddress);
        return baseAmount - (baseAmount * tier.JobFeeDiscount);
    }

    public async Task<bool> CanCreateProposalAsync(string walletAddress, int proposalsThisMonth)
    {
        var tier = await GetUserTierAsync(walletAddress);
        return proposalsThisMonth < tier.MaxProposalsPerMonth;
    }

    public async Task<bool> CanAccessJobAsync(string walletAddress, Job job)
    {
        var tier = await GetUserTierAsync(walletAddress);

        if (!Enum.TryParse<TierLevel>(job.MinimumTierRequired, true, out var requiredTier))
            requiredTier = TierLevel.Bronze;

        if (tier.Level < requiredTier) return false;
        if (job.IsPremium && !tier.CanAccessPremiumJobs) return false;

        return true;
    }

    public List<ReputationTier> GetAllTiers() => ReputationTier.GetAllTiers();

    public async Task<Dictionary<string, object>> GetUserDashboardDataAsync(string walletAddress)
    {
        var scoreData = await _fairScoreService.GetScoreAsync(walletAddress);
        var tier = !string.IsNullOrWhiteSpace(scoreData.Tier)
            ? ReputationTier.GetTierByName(scoreData.Tier)
            : ReputationTier.GetTierByScore(scoreData.Score);

        var votingPower = await CalculateVotingPowerAsync(walletAddress);
        var suggestions = await _fairScoreService.GetImprovementSuggestionsAsync(walletAddress);

        return new Dictionary<string, object>
        {
            ["score"] = scoreData.Score,
            ["fairscoreBase"] = scoreData.FairscoreBase,
            ["socialScore"] = scoreData.SocialScore,
            ["fairscore"] = scoreData.Fairscore,
            ["tier"] = tier,
            ["tierName"] = scoreData.Tier,
            ["votingPower"] = votingPower,
            ["badges"] = scoreData.Badges,
            ["features"] = scoreData.Features,
            ["history"] = scoreData.History,
            ["suggestions"] = suggestions,
            ["nextTier"] = GetNextTier(tier)!,
            ["scoreToNextTier"] = GetScoreToNextTier(scoreData.Score, tier)
        };
    }

    private ReputationTier? GetNextTier(ReputationTier currentTier)
    {
        var allTiers = ReputationTier.GetAllTiers();
        var currentIndex = allTiers.FindIndex(t => t.Level == currentTier.Level);
        if (currentIndex >= 0 && currentIndex < allTiers.Count - 1)
            return allTiers[currentIndex + 1];
        return null;
    }

    private int GetScoreToNextTier(int currentScore, ReputationTier currentTier)
    {
        var nextTier = GetNextTier(currentTier);
        if (nextTier == null) return 0;
        return Math.Max(0, nextTier.MinScore - currentScore);
    }
}
