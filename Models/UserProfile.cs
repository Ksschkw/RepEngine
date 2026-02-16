namespace RepEngine.Models;

public class UserProfile
{
    public string WalletAddress { get; set; } = string.Empty;
    public int CurrentScore { get; set; }
    public TierLevel CurrentTier { get; set; }
    public DateTime LastScoreUpdate { get; set; }
    public int TotalVotesCast { get; set; }
    public int ProposalsCreated { get; set; }
    public int JobsCompleted { get; set; }
    public decimal TotalEarnings { get; set; }
    public bool IsVerified { get; set; }
    public DateTime JoinedDate { get; set; }
}
