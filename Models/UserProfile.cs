using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepEngine.Models;

public class UserProfile
{
    [Key]
    public string WalletAddress { get; set; } = string.Empty;

    // ── Identity ──────────────────────────────────────────
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;          // e.g. "Smart Contract Developer"

    // ── Skills & Portfolio ────────────────────────────────
    public List<string> Skills { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public string PortfolioUrl { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string TwitterUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;

    // ── Freelancer Preferences ────────────────────────────
    public decimal HourlyRate { get; set; }
    public string AvailabilityStatus { get; set; } = "Available";  // Available | Busy | Not Available
    public string Timezone { get; set; } = "UTC";

    // ── Reputation (cached from FairScore) ────────────────
    public int CurrentScore { get; set; }
    public TierLevel CurrentTier { get; set; }
    public DateTime LastScoreUpdate { get; set; }

    // ── Activity Stats ────────────────────────────────────
    public int TotalVotesCast { get; set; }
    public int ProposalsCreated { get; set; }
    public int JobsCompleted { get; set; }
    public int JobsPosted { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalSpent { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // ── Profile Completion ────────────────────────────────
    public int ProfileCompletionPercent { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }

    // ── Dates ─────────────────────────────────────────────
    public DateTime JoinedDate { get; set; }
    public DateTime LastActiveDate { get; set; }
}
