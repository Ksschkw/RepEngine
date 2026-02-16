namespace RepEngine.Models;

public class FairScoreResponse
{
    public string WalletAddress { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Tier { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public ScoreBreakdown Breakdown { get; set; } = new();
    public List<HistoricalScore> History { get; set; } = new();
}

public class ScoreBreakdown
{
    public int TransactionVolume { get; set; }
    public int AccountAge { get; set; }
    public int DeFiActivity { get; set; }
    public int GovernanceParticipation { get; set; }
    public int SocialReputation { get; set; }
}

public class HistoricalScore
{
    public DateTime Date { get; set; }
    public int Score { get; set; }
}
