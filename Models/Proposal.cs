namespace RepEngine.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CreatorWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime VotingEndsAt { get; set; }
        public string Status { get; set; } = "Active";
        public int VotesFor { get; set; }
        public int VotesAgainst { get; set; }
        public int TotalVotingPower { get; set; }
        public int TotalVoters { get; set; }
    }

    public class Vote
    {
        public int Id { get; set; }
        public int ProposalId { get; set; }
        public string VoterWallet { get; set; } = string.Empty;
        public bool InFavor { get; set; }
        public int VotingPower { get; set; }
        public DateTime VotedAt { get; set; }
    }
}
