namespace RepEngine.Models;

/// <summary>
/// Maps the real FairScale /score API response.
/// Docs: https://docs.fairscale.xyz/docs/api-score
/// </summary>
public class FairScoreResponse
{
    /// <summary>Solana wallet address</summary>
    public string WalletAddress { get; set; } = string.Empty;

    // ── Real FairScale API fields ──────────────────────────────────────────
    /// <summary>Base FairScore without social factors</summary>
    public double FairscoreBase { get; set; }

    /// <summary>Social reputation score</summary>
    public double SocialScore { get; set; }

    /// <summary>Combined FairScore (wallet + social)</summary>
    public double Fairscore { get; set; }

    /// <summary>Tier string from the API: "bronze", "silver", "gold", "platinum"</summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>Earned badges</summary>
    public List<FairScoreBadge> Badges { get; set; } = new();

    /// <summary>Detailed on-chain feature breakdown</summary>
    public FairScoreFeatures Features { get; set; } = new();

    /// <summary>When the score was calculated</summary>
    public DateTime LastUpdated { get; set; }

    // ── Derived / computed ─────────────────────────────────────────────────
    /// <summary>
    /// Integer score (0–100) used for tier mapping and UI gauges.
    /// Maps the API's fairscore (typically 0–150+) to 0–100 for display.
    /// </summary>
    public int Score => Math.Min(100, (int)Math.Round(Fairscore));

    /// <summary>Score history for charts (app-generated for now)</summary>
    public List<HistoricalScore> History { get; set; } = new();
}

public class FairScoreBadge
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeTier { get; set; } = string.Empty;
}

public class FairScoreFeatures
{
    public double LstPercentileScore { get; set; }
    public double MajorPercentileScore { get; set; }
    public double NativeSolPercentile { get; set; }
    public double StablePercentileScore { get; set; }
    public int TxCount { get; set; }
    public int ActiveDays { get; set; }
    public double MedianGapHours { get; set; }
    public int WalletAgeDays { get; set; }
}

public class HistoricalScore
{
    public DateTime Date { get; set; }
    public int Score { get; set; }
}
