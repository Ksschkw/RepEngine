using RepEngine.Models;
using RepEngine.Data;
using Microsoft.EntityFrameworkCore;

namespace RepEngine.Services;

public class GovernanceService
{
    private readonly ReputationService _reputationService;
    private readonly FairScoreService _fairScoreService;
    private readonly RepEngineContext _context;
    private readonly ILogger<GovernanceService> _logger;

    public GovernanceService(ReputationService reputationService, FairScoreService fairScoreService, RepEngineContext context, ILogger<GovernanceService> logger)
    {
        _reputationService = reputationService;
        _fairScoreService = fairScoreService;
        _context = context;
        _logger = logger;
    }

    public async Task<Proposal> CreateProposalAsync(string creatorWallet, string title, string description, string category, int votingDurationDays, string minimumTierToVote = "Unranked", int quorumRequired = 100)
    {
        var canCreate = await _reputationService.CanCreateProposalAsync(creatorWallet, await GetUserProposalsThisMonthAsync(creatorWallet));

        if (!canCreate)
            throw new InvalidOperationException("You don't have permission to create more proposals this month");

        // Capture creator's FairScore
        int fairScore = 0;
        string tierName = "Unranked";
        try
        {
            var scoreData = await _fairScoreService.GetScoreAsync(creatorWallet);
            fairScore = scoreData.Score;
            tierName = scoreData.Tier ?? "Unranked";
        }
        catch { }

        var proposal = new Proposal
        {
            Title = title,
            Description = description,
            CreatorWallet = creatorWallet,
            Category = category,
            MinimumTierToVote = minimumTierToVote,
            QuorumRequired = quorumRequired,
            CreatorFairScore = fairScore,
            CreatorTier = tierName,
            CreatedAt = DateTime.UtcNow,
            VotingEndsAt = DateTime.UtcNow.AddDays(votingDurationDays),
            Status = "Active"
        };

        _context.Proposals.Add(proposal);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Proposal {ProposalId} created by {Wallet} (FairScore: {Score})", proposal.Id, creatorWallet, fairScore);
        return proposal;
    }

    public async Task<Vote> CastVoteAsync(int proposalId, string voterWallet, bool inFavor)
    {
        var proposal = await _context.Proposals.FirstOrDefaultAsync(p => p.Id == proposalId);
        if (proposal == null)
            throw new ArgumentException("Proposal not found");

        if (proposal.Status != "Active")
            throw new InvalidOperationException("Proposal is not active");

        if (DateTime.UtcNow > proposal.VotingEndsAt)
            throw new InvalidOperationException("Voting period has ended");

        // Check if user already voted
        if (await _context.Votes.AnyAsync(v => v.ProposalId == proposalId && v.VoterWallet == voterWallet))
            throw new InvalidOperationException("You have already voted on this proposal");

        // Check tier requirement for voting
        if (proposal.MinimumTierToVote != "Unranked")
        {
            var canVote = await _reputationService.CanAccessFeatureAsync(voterWallet,
                Enum.TryParse<TierLevel>(proposal.MinimumTierToVote, true, out var reqTier) ? reqTier : TierLevel.Unranked);
            if (!canVote)
                throw new InvalidOperationException($"Your tier must be at least {proposal.MinimumTierToVote} to vote on this proposal");
        }

        var votingPower = await _reputationService.CalculateVotingPowerAsync(voterWallet);

        // Capture voter's FairScore
        int fairScore = 0;
        string tierName = "Unranked";
        try
        {
            var scoreData = await _fairScoreService.GetScoreAsync(voterWallet);
            fairScore = scoreData.Score;
            tierName = scoreData.Tier ?? "Unranked";
        }
        catch { }

        var vote = new Vote
        {
            ProposalId = proposalId,
            VoterWallet = voterWallet,
            InFavor = inFavor,
            VotingPower = (int)votingPower,
            VoterFairScore = fairScore,
            VoterTier = tierName,
            VotedAt = DateTime.UtcNow
        };

        _context.Votes.Add(vote);

        // Update proposal vote counts
        if (inFavor)
            proposal.VotesFor += (int)votingPower;
        else
            proposal.VotesAgainst += (int)votingPower;

        proposal.TotalVotingPower += (int)votingPower;
        proposal.TotalVoters++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vote cast on proposal {ProposalId} by {Wallet} (FairScore: {Score}, Power: {Power})",
            proposalId, voterWallet, fairScore, votingPower);

        return vote;
    }

    public async Task<List<Proposal>> GetActiveProposalsAsync()
    {
        await UpdateProposalStatusesAsync();
        return await _context.Proposals
            .Where(p => p.Status == "Active")
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Proposal>> GetAllProposalsAsync()
    {
        await UpdateProposalStatusesAsync();
        return await _context.Proposals
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Proposal?> GetProposalAsync(int proposalId)
    {
        await UpdateProposalStatusesAsync();
        return await _context.Proposals.FirstOrDefaultAsync(p => p.Id == proposalId);
    }

    public async Task<List<Vote>> GetProposalVotesAsync(int proposalId)
    {
        return await _context.Votes
            .Where(v => v.ProposalId == proposalId)
            .OrderByDescending(v => v.VotedAt)
            .ToListAsync();
    }

    public async Task<bool> HasUserVotedAsync(int proposalId, string walletAddress)
    {
        return await _context.Votes.AnyAsync(v => v.ProposalId == proposalId && v.VoterWallet == walletAddress);
    }

    private async Task UpdateProposalStatusesAsync()
    {
        var activeProposals = await _context.Proposals.Where(p => p.Status == "Active").ToListAsync();
        bool changed = false;

        foreach (var proposal in activeProposals)
        {
            if (DateTime.UtcNow > proposal.VotingEndsAt)
            {
                // Check quorum
                bool quorumMet = proposal.TotalVotingPower >= proposal.QuorumRequired;
                if (!quorumMet)
                {
                    proposal.Status = "Rejected"; // No quorum
                }
                else
                {
                    proposal.Status = proposal.VotesFor > proposal.VotesAgainst ? "Passed" : "Rejected";
                }
                changed = true;
            }
        }

        if (changed) await _context.SaveChangesAsync();
    }

    private async Task<int> GetUserProposalsThisMonthAsync(string walletAddress)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _context.Proposals.CountAsync(p => p.CreatorWallet == walletAddress && p.CreatedAt >= startOfMonth);
    }

    // ── Governance Stats ──────────────────────────────────────
    public async Task<object> GetGovernanceStatsAsync()
    {
        return new
        {
            totalProposals = await _context.Proposals.CountAsync(),
            activeProposals = await _context.Proposals.CountAsync(p => p.Status == "Active"),
            passedProposals = await _context.Proposals.CountAsync(p => p.Status == "Passed"),
            totalVotes = await _context.Votes.CountAsync(),
            totalVotingPower = await _context.Votes.SumAsync(v => v.VotingPower),
            uniqueVoters = await _context.Votes.Select(v => v.VoterWallet).Distinct().CountAsync()
        };
    }
}
