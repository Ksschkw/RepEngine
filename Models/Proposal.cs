using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RepEngine.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string CreatorWallet { get; set; } = string.Empty;
        [ForeignKey("CreatorWallet")]
        [JsonIgnore]
        public UserProfile? Creator { get; set; }

        // Governance settings
        public string MinimumTierToVote { get; set; } = "Unranked";   // FairScore tier gating for voting
        public int MinimumScoreToVote { get; set; } = 0;
        public int QuorumRequired { get; set; } = 100;                // Min total voting power needed

        // Vote tracking
        public int VotesFor { get; set; }
        public int VotesAgainst { get; set; }
        public int TotalVotingPower { get; set; }
        public int TotalVoters { get; set; }

        // Status & timing
        public DateTime CreatedAt { get; set; }
        public DateTime VotingEndsAt { get; set; }
        public string Status { get; set; } = "Active";    // Active | Passed | Rejected | Executed

        // Creator FairScore snapshot
        public int CreatorFairScore { get; set; }
        public string CreatorTier { get; set; } = "Unranked";
    }

    public class Vote
    {
        public int Id { get; set; }

        public int ProposalId { get; set; }
        [ForeignKey("ProposalId")]
        [JsonIgnore]
        public Proposal? Proposal { get; set; }

        public string VoterWallet { get; set; } = string.Empty;
        public bool InFavor { get; set; }
        public int VotingPower { get; set; }

        // FairScore snapshot at vote time
        public int VoterFairScore { get; set; }
        public string VoterTier { get; set; } = "Unranked";

        public DateTime VotedAt { get; set; }
    }
}
