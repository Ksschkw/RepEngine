using RepEngine.Models;

namespace RepEngine.Services;

public class GovernanceService
{
    private readonly ReputationService _reputationService;
    private readonly ILogger<GovernanceService> _logger;
    private static List<Proposal> _proposals = new(); // In-memory storage for demo
    private static List<Vote> _votes = new();
    private static int _nextProposalId = 1;

    public GovernanceService(ReputationService reputationService, ILogger<GovernanceService> logger)
    {
        _reputationService = reputationService;
        _logger = logger;
        InitializeSampleProposals();
    }

    public async Task<Proposal> CreateProposalAsync(string creatorWallet, string title, string description, string category, int votingDurationDays)
    {
        var canCreate = await _reputationService.CanCreateProposalAsync(creatorWallet, GetUserProposalsThisMonth(creatorWallet));
        
        if (!canCreate)
            throw new InvalidOperationException("You don't have permission to create more proposals this month");

        var proposal = new Proposal
        {
            Id = _nextProposalId++,
            Title = title,
            Description = description,
            CreatorWallet = creatorWallet,
            CreatedAt = DateTime.UtcNow,
            VotingEndsAt = DateTime.UtcNow.AddDays(votingDurationDays),
            Status = "Active",
            Category = category
        };

        _proposals.Add(proposal);
        _logger.LogInformation("Proposal {ProposalId} created by {Wallet}", proposal.Id, creatorWallet);
        
        return proposal;
    }

    public async Task<Vote> CastVoteAsync(int proposalId, string voterWallet, bool inFavor)
    {
        var proposal = _proposals.FirstOrDefault(p => p.Id == proposalId);
        if (proposal == null)
            throw new ArgumentException("Proposal not found");

        if (proposal.Status != "Active")
            throw new InvalidOperationException("Proposal is not active");

        if (DateTime.UtcNow > proposal.VotingEndsAt)
            throw new InvalidOperationException("Voting period has ended");

        // Check if user already voted
        if (_votes.Any(v => v.ProposalId == proposalId && v.VoterWallet == voterWallet))
            throw new InvalidOperationException("You have already voted on this proposal");

        var votingPower = await _reputationService.CalculateVotingPowerAsync(voterWallet);

        var vote = new Vote
        {
            ProposalId = proposalId,
            VoterWallet = voterWallet,
            InFavor = inFavor,
            VotingPower = (int)votingPower,
            VotedAt = DateTime.UtcNow
        };

        _votes.Add(vote);

        // Update proposal vote counts
        if (inFavor)
            proposal.VotesFor += (int)votingPower;
        else
            proposal.VotesAgainst += (int)votingPower;

        proposal.TotalVotingPower += (int)votingPower;
        proposal.TotalVoters++;

        _logger.LogInformation("Vote cast on proposal {ProposalId} by {Wallet} with power {Power}", 
            proposalId, voterWallet, votingPower);

        return vote;
    }

    public List<Proposal> GetActiveProposals()
    {
        UpdateProposalStatuses();
        return _proposals.Where(p => p.Status == "Active").OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<Proposal> GetAllProposals()
    {
        UpdateProposalStatuses();
        return _proposals.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public Proposal? GetProposal(int proposalId)
    {
        UpdateProposalStatuses();
        return _proposals.FirstOrDefault(p => p.Id == proposalId);
    }

    public List<Vote> GetProposalVotes(int proposalId)
    {
        return _votes.Where(v => v.ProposalId == proposalId).OrderByDescending(v => v.VotedAt).ToList();
    }

    public bool HasUserVoted(int proposalId, string walletAddress)
    {
        return _votes.Any(v => v.ProposalId == proposalId && v.VoterWallet == walletAddress);
    }

    private void UpdateProposalStatuses()
    {
        foreach (var proposal in _proposals.Where(p => p.Status == "Active"))
        {
            if (DateTime.UtcNow > proposal.VotingEndsAt)
            {
                // Determine if proposal passed (simple majority of voting power)
                proposal.Status = proposal.VotesFor > proposal.VotesAgainst 
                    ? "Passed" 
                    : "Rejected";
            }
        }
    }

    private int GetUserProposalsThisMonth(string walletAddress)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return _proposals.Count(p => p.CreatorWallet == walletAddress && p.CreatedAt >= startOfMonth);
    }

    private void InitializeSampleProposals()
    {
        if (_proposals.Any()) return;

        _proposals.Add(new Proposal
        {
            Id = _nextProposalId++,
            Title = "Reduce Platform Fees by 2%",
            Description = "Proposal to reduce the platform fee from 5% to 3% to attract more freelancers and clients.",
            CreatorWallet = "Demo1...",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            VotingEndsAt = DateTime.UtcNow.AddDays(2),
            Status = "Active",
            Category = "Platform Economics",
            VotesFor = 1250,
            VotesAgainst = 340,
            TotalVotingPower = 1590,
            TotalVoters = 47
        });

        _proposals.Add(new Proposal
        {
            Id = _nextProposalId++,
            Title = "Introduce Reputation Staking",
            Description = "Allow users to stake tokens to boost their reputation score temporarily for important proposals or job applications.",
            CreatorWallet = "Demo2...",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            VotingEndsAt = DateTime.UtcNow.AddDays(4),
            Status = "Active",
            Category = "Reputation System",
            VotesFor = 890,
            VotesAgainst = 560,
            TotalVotingPower = 1450,
            TotalVoters = 38
        });

        _proposals.Add(new Proposal
        {
            Id = _nextProposalId++,
            Title = "Add Dispute Resolution System",
            Description = "Implement a decentralized dispute resolution mechanism for job conflicts, with arbitrators selected based on reputation.",
            CreatorWallet = "Demo3...",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            VotingEndsAt = DateTime.UtcNow.AddDays(6),
            Status = "Active",
            Category = "Platform Features",
            VotesFor = 420,
            VotesAgainst = 180,
            TotalVotingPower = 600,
            TotalVoters = 22
        });
    }
}
