namespace RepEngine.Models;

public enum TierLevel
{
    Unranked = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5
}

public class ReputationTier
{
    public TierLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Benefits { get; set; } = new();
    public decimal VotingPowerMultiplier { get; set; }
    public int MaxProposalsPerMonth { get; set; }
    public bool CanAccessPremiumJobs { get; set; }
    public decimal JobFeeDiscount { get; set; }

    public static List<ReputationTier> GetAllTiers()
    {
        return new List<ReputationTier>
        {
            new ReputationTier
            {
                Level = TierLevel.Unranked,
                Name = "Unranked",
                MinScore = 0,
                MaxScore = 19,
                Color = "#6B7280",
                Icon = "⚪",
                Benefits = new List<string> { "Basic platform access", "View public proposals" },
                VotingPowerMultiplier = 0.5m,
                MaxProposalsPerMonth = 0,
                CanAccessPremiumJobs = false,
                JobFeeDiscount = 0m
            },
            new ReputationTier
            {
                Level = TierLevel.Bronze,
                Name = "Bronze",
                MinScore = 20,
                MaxScore = 39,
                Color = "#CD7F32",
                Icon = "🥉",
                Benefits = new List<string> { "Vote on proposals", "Access entry-level jobs", "5% fee discount" },
                VotingPowerMultiplier = 1.0m,
                MaxProposalsPerMonth = 1,
                CanAccessPremiumJobs = false,
                JobFeeDiscount = 0.05m
            },
            new ReputationTier
            {
                Level = TierLevel.Silver,
                Name = "Silver",
                MinScore = 40,
                MaxScore = 59,
                Color = "#C0C0C0",
                Icon = "🥈",
                Benefits = new List<string> { "1.5x voting power", "Create proposals", "Access mid-tier jobs", "10% fee discount" },
                VotingPowerMultiplier = 1.5m,
                MaxProposalsPerMonth = 3,
                CanAccessPremiumJobs = false,
                JobFeeDiscount = 0.10m
            },
            new ReputationTier
            {
                Level = TierLevel.Gold,
                Name = "Gold",
                MinScore = 60,
                MaxScore = 79,
                Color = "#FFD700",
                Icon = "🥇",
                Benefits = new List<string> { "2x voting power", "Access premium jobs", "Priority support", "15% fee discount" },
                VotingPowerMultiplier = 2.0m,
                MaxProposalsPerMonth = 5,
                CanAccessPremiumJobs = true,
                JobFeeDiscount = 0.15m
            },
            new ReputationTier
            {
                Level = TierLevel.Platinum,
                Name = "Platinum",
                MinScore = 80,
                MaxScore = 94,
                Color = "#E5E4E2",
                Icon = "💎",
                Benefits = new List<string> { "3x voting power", "Exclusive job access", "Featured profile", "20% fee discount" },
                VotingPowerMultiplier = 3.0m,
                MaxProposalsPerMonth = 10,
                CanAccessPremiumJobs = true,
                JobFeeDiscount = 0.20m
            },
            new ReputationTier
            {
                Level = TierLevel.Diamond,
                Name = "Diamond",
                MinScore = 95,
                MaxScore = 100,
                Color = "#B9F2FF",
                Icon = "💠",
                Benefits = new List<string> { "5x voting power", "Unlimited proposals", "VIP job access", "25% fee discount", "Revenue sharing" },
                VotingPowerMultiplier = 5.0m,
                MaxProposalsPerMonth = 999,
                CanAccessPremiumJobs = true,
                JobFeeDiscount = 0.25m
            }
        };
    }

    public static ReputationTier GetTierByScore(int score)
    {
        var tiers = GetAllTiers();
        return tiers.FirstOrDefault(t => score >= t.MinScore && score <= t.MaxScore) 
               ?? tiers.First();
    }
}
