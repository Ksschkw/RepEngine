using RepEngine.Models;
using RepEngine.Data;
using Microsoft.EntityFrameworkCore;

namespace RepEngine.Services;

public class JobService
{
    private readonly ReputationService _reputationService;
    private readonly FairScoreService _fairScoreService;
    private readonly RepEngineContext _context;
    private readonly ILogger<JobService> _logger;

    public JobService(ReputationService reputationService, FairScoreService fairScoreService, RepEngineContext context, ILogger<JobService> logger)
    {
        _reputationService = reputationService;
        _fairScoreService = fairScoreService;
        _context = context;
        _logger = logger;
    }

    // ── User Profile ──────────────────────────────────────────
    private async Task EnsureUserProfileExists(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress)) return;
        var exists = await _context.UserProfiles.AnyAsync(u => u.WalletAddress == walletAddress);
        if (!exists)
        {
            _context.UserProfiles.Add(new UserProfile
            {
                WalletAddress = walletAddress,
                JoinedDate = DateTime.UtcNow,
                LastActiveDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update last active
            var profile = await _context.UserProfiles.FindAsync(walletAddress);
            if (profile != null) { profile.LastActiveDate = DateTime.UtcNow; await _context.SaveChangesAsync(); }
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string walletAddress)
    {
        await EnsureUserProfileExists(walletAddress);
        return await _context.UserProfiles.FindAsync(walletAddress);
    }

    public async Task<UserProfile> UpdateUserProfileAsync(string walletAddress, UserProfile updates)
    {
        await EnsureUserProfileExists(walletAddress);
        var profile = await _context.UserProfiles.FindAsync(walletAddress);
        if (profile == null) throw new ArgumentException("Profile not found");

        profile.DisplayName = updates.DisplayName ?? profile.DisplayName;
        profile.Bio = updates.Bio ?? profile.Bio;
        profile.Title = updates.Title ?? profile.Title;
        profile.AvatarUrl = updates.AvatarUrl ?? profile.AvatarUrl;
        profile.Skills = updates.Skills?.Count > 0 ? updates.Skills : profile.Skills;
        profile.Languages = updates.Languages?.Count > 0 ? updates.Languages : profile.Languages;
        profile.PortfolioUrl = updates.PortfolioUrl ?? profile.PortfolioUrl;
        profile.GithubUrl = updates.GithubUrl ?? profile.GithubUrl;
        profile.TwitterUrl = updates.TwitterUrl ?? profile.TwitterUrl;
        profile.WebsiteUrl = updates.WebsiteUrl ?? profile.WebsiteUrl;
        profile.HourlyRate = updates.HourlyRate > 0 ? updates.HourlyRate : profile.HourlyRate;
        profile.AvailabilityStatus = updates.AvailabilityStatus ?? profile.AvailabilityStatus;
        profile.Timezone = updates.Timezone ?? profile.Timezone;
        profile.LastActiveDate = DateTime.UtcNow;

        // Calculate completion
        profile.ProfileCompletionPercent = CalculateProfileCompletion(profile);

        await _context.SaveChangesAsync();
        return profile;
    }

    private int CalculateProfileCompletion(UserProfile p)
    {
        int score = 0;
        if (!string.IsNullOrEmpty(p.DisplayName)) score += 15;
        if (!string.IsNullOrEmpty(p.Bio)) score += 15;
        if (!string.IsNullOrEmpty(p.Title)) score += 10;
        if (p.Skills.Count > 0) score += 15;
        if (!string.IsNullOrEmpty(p.AvatarUrl)) score += 10;
        if (p.HourlyRate > 0) score += 10;
        if (!string.IsNullOrEmpty(p.PortfolioUrl) || !string.IsNullOrEmpty(p.GithubUrl)) score += 15;
        if (p.Languages.Count > 0) score += 10;
        return Math.Min(100, score);
    }

    // ── Jobs ──────────────────────────────────────────────────
    public async Task<List<Job>> GetAvailableJobsAsync(string walletAddress)
    {
        var allJobs = await _context.Jobs.Where(j => j.Status == "Open").ToListAsync();
        var accessibleJobs = new List<Job>();

        foreach (var job in allJobs)
        {
            if (await CanAccessJobAsync(walletAddress, job))
            {
                accessibleJobs.Add(job);
            }
        }

        return accessibleJobs.OrderByDescending(j => j.PostedAt).ToList();
    }

    public async Task<bool> CanAccessJobAsync(string walletAddress, Job job)
    {
        // Public visibility: check tier only
        if (job.Visibility == "Public")
            return await _reputationService.CanAccessJobAsync(walletAddress, job);

        // TierGated: must meet tier AND minimum FairScore
        if (job.Visibility == "TierGated")
        {
            if (!await _reputationService.CanAccessJobAsync(walletAddress, job))
                return false;

            if (job.MinimumFairScore > 0)
            {
                try
                {
                    var score = await _fairScoreService.GetScoreAsync(walletAddress);
                    if (score.Score < job.MinimumFairScore) return false;
                }
                catch { return false; }
            }
            return true;
        }

        // Private: only the poster can see it
        return job.ClientWallet == walletAddress;
    }

    public async Task<List<Job>> GetAllJobsAsync()
    {
        return await _context.Jobs.OrderByDescending(j => j.PostedAt).ToListAsync();
    }

    public async Task<List<Job>> SearchJobsAsync(string? query, string? category, string? tier, string? budgetType, string? experienceLevel)
    {
        var jobs = _context.Jobs.Where(j => j.Status == "Open").AsQueryable();

        if (!string.IsNullOrEmpty(query))
            jobs = jobs.Where(j => j.Title.Contains(query) || j.Description.Contains(query));

        if (!string.IsNullOrEmpty(category) && category != "All")
            jobs = jobs.Where(j => j.Category == category);

        if (!string.IsNullOrEmpty(tier) && tier != "All")
            jobs = jobs.Where(j => j.MinimumTierRequired == tier);

        if (!string.IsNullOrEmpty(budgetType) && budgetType != "All")
            jobs = jobs.Where(j => j.BudgetType == budgetType);

        if (!string.IsNullOrEmpty(experienceLevel) && experienceLevel != "All")
            jobs = jobs.Where(j => j.ExperienceLevel == experienceLevel);

        return await jobs.OrderByDescending(j => j.PostedAt).ToListAsync();
    }

    public async Task<Job?> GetJobAsync(int jobId)
    {
        return await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        await EnsureUserProfileExists(job.ClientWallet);
        job.PostedAt = DateTime.UtcNow;
        job.Status = "Open";
        _context.Jobs.Add(job);

        // Update user's job posted count
        var profile = await _context.UserProfiles.FindAsync(job.ClientWallet);
        if (profile != null) profile.JobsPosted++;

        await _context.SaveChangesAsync();

        // Create notification
        await CreateNotificationAsync(job.ClientWallet, "Job Posted",
            $"Your job '{job.Title}' has been posted successfully!", $"/Marketplace", "Info");

        return job;
    }

    // ── Applications ──────────────────────────────────────────
    public async Task<JobApplication> ApplyToJobAsync(int jobId, string freelancerWallet, string coverLetter, decimal proposedRate, int deliveryDays = 0, string portfolioLinks = "", string availability = "Immediate")
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
            throw new ArgumentException("Job not found");

        if (job.Status != "Open")
            throw new InvalidOperationException("Job is not open for applications");

        if (!await CanAccessJobAsync(freelancerWallet, job))
            throw new InvalidOperationException("Your FairScore/tier is not high enough for this job");

        var hasApplied = await _context.JobApplications.AnyAsync(a => a.JobId == jobId && a.FreelancerWallet == freelancerWallet);
        if (hasApplied)
            throw new InvalidOperationException("You have already applied to this job");

        await EnsureUserProfileExists(freelancerWallet);

        // Capture FairScore snapshot
        int fairScore = 0;
        string tierName = "Unranked";
        try
        {
            var scoreData = await _fairScoreService.GetScoreAsync(freelancerWallet);
            fairScore = scoreData.Score;
            tierName = scoreData.Tier ?? "Unranked";
        }
        catch { /* continue without score snapshot */ }

        var application = new JobApplication
        {
            JobId = jobId,
            FreelancerWallet = freelancerWallet,
            CoverLetter = coverLetter,
            ProposedRate = proposedRate,
            DeliveryDays = deliveryDays > 0 ? deliveryDays : 3,
            PortfolioLinks = portfolioLinks,
            Availability = availability,
            FairScoreAtApplication = fairScore,
            TierAtApplication = tierName,
            AppliedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        _context.JobApplications.Add(application);
        job.ApplicationCount++;

        await _context.SaveChangesAsync();

        // Notify the job poster
        await CreateNotificationAsync(job.ClientWallet, "New Application",
            $"You received a new application for '{job.Title}' from {freelancerWallet.Substring(0, 6)}...",
            $"/Dashboard", "Application");

        _logger.LogInformation("Application {AppId} submitted for job {JobId} by {Wallet}",
            application.Id, jobId, freelancerWallet);

        return application;
    }

    public async Task<List<JobApplication>> GetJobApplicationsAsync(int jobId)
    {
        return await _context.JobApplications.Where(a => a.JobId == jobId).OrderByDescending(a => a.AppliedAt).ToListAsync();
    }

    public async Task<List<JobApplication>> GetFreelancerApplicationsAsync(string walletAddress)
    {
        return await _context.JobApplications.Include(a => a.Job).Where(a => a.FreelancerWallet == walletAddress).OrderByDescending(a => a.AppliedAt).ToListAsync();
    }

    public async Task<JobApplication> ShortlistApplicationAsync(int applicationId, string clientWallet)
    {
        var application = await _context.JobApplications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null) throw new ArgumentException("Application not found");
        if (application.Job!.ClientWallet != clientWallet) throw new UnauthorizedAccessException("Only the job poster can shortlist");

        application.Status = "Shortlisted";
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(application.FreelancerWallet, "Shortlisted! 🎉",
            $"You've been shortlisted for '{application.Job.Title}'", "/Dashboard", "Application");

        return application;
    }

    public async Task<JobApplication> RejectApplicationAsync(int applicationId, string clientWallet)
    {
        var application = await _context.JobApplications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null) throw new ArgumentException("Application not found");
        if (application.Job!.ClientWallet != clientWallet) throw new UnauthorizedAccessException("Only the job poster can reject");

        application.Status = "Rejected";
        await _context.SaveChangesAsync();
        return application;
    }

    // ── Contracts ─────────────────────────────────────────────
    public async Task<Contract> ApproveApplicationAsync(int applicationId, string clientWallet)
    {
        var application = await _context.JobApplications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null) throw new ArgumentException("Application not found");
        if (application.Job!.ClientWallet != clientWallet) throw new UnauthorizedAccessException("Only the job poster can approve applications");
        if (application.Status == "Approved") throw new InvalidOperationException("Application is already approved");

        application.Status = "Approved";
        application.Job.Status = "In Progress";

        // Reject other pending applications
        var otherApps = await _context.JobApplications
            .Where(a => a.JobId == application.JobId && a.Id != applicationId && a.Status == "Pending")
            .ToListAsync();

        foreach (var other in otherApps)
            other.Status = "Rejected";

        // Calculate platform fee based on freelancer tier
        var feeDiscount = 0m;
        try
        {
            var tier = await _reputationService.GetUserTierAsync(application.FreelancerWallet);
            feeDiscount = tier.JobFeeDiscount;
        }
        catch { }

        var baseFee = 0.05m; // 5% default
        var effectiveFee = baseFee - (baseFee * feeDiscount);

        // Create contract
        var contract = new Contract
        {
            JobId = application.JobId,
            ApplicationId = applicationId,
            ClientWallet = clientWallet,
            FreelancerWallet = application.FreelancerWallet,
            AgreedAmount = application.ProposedRate,
            PaymentCurrency = application.Job.PaymentCurrency,
            PlatformFee = effectiveFee,
            StartDate = DateTime.UtcNow,
            DeadlineDate = DateTime.UtcNow.AddDays(application.DeliveryDays),
            Status = "Active"
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Create default milestone
        _context.Milestones.Add(new Milestone
        {
            ContractId = contract.Id,
            Title = "Full Delivery",
            Description = $"Complete delivery of: {application.Job.Title}",
            Amount = application.ProposedRate,
            OrderIndex = 1,
            DueDate = contract.DeadlineDate,
            Status = "In Progress"
        });

        await _context.SaveChangesAsync();

        // Notifications
        await CreateNotificationAsync(application.FreelancerWallet, "You're Hired! 🎉",
            $"Your application for '{application.Job.Title}' has been approved! Contract #{contract.Id} created.",
            "/Dashboard", "Contract");

        _logger.LogInformation("Application {AppId} approved, Contract {ContractId} created", applicationId, contract.Id);
        return contract;
    }

    public async Task<List<Contract>> GetUserContractsAsync(string walletAddress)
    {
        return await _context.Contracts.Include(c => c.Job)
            .Where(c => c.ClientWallet == walletAddress || c.FreelancerWallet == walletAddress)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<Contract?> GetContractAsync(int contractId)
    {
        return await _context.Contracts.Include(c => c.Job).Include(c => c.Milestones)
            .FirstOrDefaultAsync(c => c.Id == contractId);
    }

    public async Task<Contract> CompleteContractAsync(int contractId, string clientWallet)
    {
        var contract = await _context.Contracts.Include(c => c.Job)
            .FirstOrDefaultAsync(c => c.Id == contractId);
        if (contract == null) throw new ArgumentException("Contract not found");
        if (contract.ClientWallet != clientWallet) throw new UnauthorizedAccessException("Only the client can complete a contract");

        contract.Status = "Completed";
        contract.CompletedDate = DateTime.UtcNow;
        contract.Job!.Status = "Completed";

        // Update profiles
        var freelancer = await _context.UserProfiles.FindAsync(contract.FreelancerWallet);
        if (freelancer != null)
        {
            freelancer.JobsCompleted++;
            freelancer.TotalEarnings += contract.AgreedAmount;
        }

        var client = await _context.UserProfiles.FindAsync(contract.ClientWallet);
        if (client != null) client.TotalSpent += contract.AgreedAmount;

        await _context.SaveChangesAsync();

        await CreateNotificationAsync(contract.FreelancerWallet, "Contract Completed ✅",
            $"Contract #{contractId} for '{contract.Job.Title}' has been completed!", "/Dashboard", "Contract");

        return contract;
    }

    // ── Reviews ───────────────────────────────────────────────
    public async Task<Review> CreateReviewAsync(int contractId, string reviewerWallet, int rating, string comment, int communication, int quality, int timeliness, int professionalism)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId);
        if (contract == null) throw new ArgumentException("Contract not found");
        if (contract.Status != "Completed") throw new InvalidOperationException("Can only review completed contracts");

        string revieweeWallet;
        string reviewerRole;

        if (contract.ClientWallet == reviewerWallet)
        {
            revieweeWallet = contract.FreelancerWallet;
            reviewerRole = "Client";
        }
        else if (contract.FreelancerWallet == reviewerWallet)
        {
            revieweeWallet = contract.ClientWallet;
            reviewerRole = "Freelancer";
        }
        else
        {
            throw new UnauthorizedAccessException("You are not part of this contract");
        }

        // Check if already reviewed
        var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
            r.ContractId == contractId && r.ReviewerWallet == reviewerWallet);
        if (alreadyReviewed) throw new InvalidOperationException("You have already reviewed this contract");

        int fairScore = 0;
        try
        {
            var score = await _fairScoreService.GetScoreAsync(reviewerWallet);
            fairScore = score.Score;
        }
        catch { }

        var review = new Review
        {
            ContractId = contractId,
            ReviewerWallet = reviewerWallet,
            RevieweeWallet = revieweeWallet,
            ReviewerRole = reviewerRole,
            Rating = Math.Clamp(rating, 1, 5),
            Comment = comment,
            Communication = Math.Clamp(communication, 1, 5),
            Quality = Math.Clamp(quality, 1, 5),
            Timeliness = Math.Clamp(timeliness, 1, 5),
            Professionalism = Math.Clamp(professionalism, 1, 5),
            ReviewerFairScore = fairScore,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        // Update reviewee's average rating
        var reviewee = await _context.UserProfiles.FindAsync(revieweeWallet);
        if (reviewee != null)
        {
            var allReviews = await _context.Reviews.Where(r => r.RevieweeWallet == revieweeWallet).ToListAsync();
            reviewee.ReviewCount = allReviews.Count + 1;
            reviewee.AverageRating = (allReviews.Sum(r => r.Rating) + rating) / (double)(allReviews.Count + 1);
        }

        await _context.SaveChangesAsync();

        await CreateNotificationAsync(revieweeWallet, "New Review ⭐",
            $"You received a {rating}-star review!", "/Dashboard", "Review");

        return review;
    }

    public async Task<List<Review>> GetUserReviewsAsync(string walletAddress)
    {
        return await _context.Reviews
            .Where(r => r.RevieweeWallet == walletAddress)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    // ── Disputes ──────────────────────────────────────────────
    public async Task<Dispute> CreateDisputeAsync(int contractId, string initiatorWallet, string reason, string evidence)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId);
        if (contract == null) throw new ArgumentException("Contract not found");
        if (contract.ClientWallet != initiatorWallet && contract.FreelancerWallet != initiatorWallet)
            throw new UnauthorizedAccessException("You are not part of this contract");

        contract.Status = "Disputed";

        var dispute = new Dispute
        {
            ContractId = contractId,
            InitiatorWallet = initiatorWallet,
            Reason = reason,
            Evidence = evidence,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync();

        // Notify the other party
        var otherWallet = contract.ClientWallet == initiatorWallet ? contract.FreelancerWallet : contract.ClientWallet;
        await CreateNotificationAsync(otherWallet, "Dispute Opened ⚠️",
            $"A dispute has been opened for contract #{contractId}", "/Dashboard", "Contract");

        return dispute;
    }

    // ── Saved Jobs ────────────────────────────────────────────
    public async Task<SavedJob> SaveJobAsync(int jobId, string walletAddress)
    {
        var existing = await _context.SavedJobs.AnyAsync(s => s.JobId == jobId && s.WalletAddress == walletAddress);
        if (existing) throw new InvalidOperationException("Job already saved");

        var saved = new SavedJob { JobId = jobId, WalletAddress = walletAddress, SavedAt = DateTime.UtcNow };
        _context.SavedJobs.Add(saved);
        await _context.SaveChangesAsync();
        return saved;
    }

    public async Task UnsaveJobAsync(int jobId, string walletAddress)
    {
        var saved = await _context.SavedJobs.FirstOrDefaultAsync(s => s.JobId == jobId && s.WalletAddress == walletAddress);
        if (saved != null) { _context.SavedJobs.Remove(saved); await _context.SaveChangesAsync(); }
    }

    public async Task<List<Job>> GetSavedJobsAsync(string walletAddress)
    {
        return await _context.SavedJobs.Where(s => s.WalletAddress == walletAddress)
            .Include(s => s.Job).Select(s => s.Job!).ToListAsync();
    }

    // ── Messages ──────────────────────────────────────────────
    public async Task<Message> SendMessageAsync(string senderWallet, string receiverWallet, string content, int? contractId = null)
    {
        var msg = new Message
        {
            SenderWallet = senderWallet,
            ReceiverWallet = receiverWallet,
            Content = content,
            ContractId = contractId,
            SentAt = DateTime.UtcNow
        };
        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(receiverWallet, "New Message 💬",
            $"You have a new message from {senderWallet.Substring(0, 6)}...", "/Dashboard", "Info");

        return msg;
    }

    public async Task<List<Message>> GetConversationAsync(string wallet1, string wallet2, int? contractId = null)
    {
        var query = _context.Messages
            .Where(m => (m.SenderWallet == wallet1 && m.ReceiverWallet == wallet2) ||
                        (m.SenderWallet == wallet2 && m.ReceiverWallet == wallet1));

        if (contractId.HasValue)
            query = query.Where(m => m.ContractId == contractId);

        return await query.OrderBy(m => m.SentAt).ToListAsync();
    }

    // ── Notifications ─────────────────────────────────────────
    public async Task CreateNotificationAsync(string walletAddress, string title, string body, string link, string type)
    {
        _context.Notifications.Add(new Notification
        {
            WalletAddress = walletAddress,
            Title = title,
            Body = body,
            Link = link,
            Type = type,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetNotificationsAsync(string walletAddress, bool unreadOnly = false)
    {
        var query = _context.Notifications.Where(n => n.WalletAddress == walletAddress);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
    }

    public async Task MarkNotificationsReadAsync(string walletAddress)
    {
        var unread = await _context.Notifications
            .Where(n => n.WalletAddress == walletAddress && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    // ── Categories ────────────────────────────────────────────
    public async Task<List<JobCategory>> GetCategoriesAsync()
    {
        var categories = await _context.JobCategories.ToListAsync();
        if (categories.Count == 0)
        {
            // Seed defaults
            var defaults = new List<JobCategory>
            {
                new() { Name = "Development", Icon = "💻", Description = "Software & Web3 development" },
                new() { Name = "Design", Icon = "🎨", Description = "UI/UX, graphic & brand design" },
                new() { Name = "Smart Contracts", Icon = "📜", Description = "Solidity, Rust, Anchor programs" },
                new() { Name = "Security", Icon = "🔒", Description = "Audits, penetration testing" },
                new() { Name = "Marketing", Icon = "📣", Description = "Growth, community, socials" },
                new() { Name = "Content", Icon = "✍️", Description = "Writing, video, documentation" },
                new() { Name = "DeFi", Icon = "🏦", Description = "Protocols, yield, liquidity" },
                new() { Name = "NFT", Icon = "🖼️", Description = "Collections, marketplaces, art" },
                new() { Name = "Data & Analytics", Icon = "📊", Description = "On-chain data, dashboards" },
                new() { Name = "Community", Icon = "🤝", Description = "Moderation, DAO ops, support" },
            };

            _context.JobCategories.AddRange(defaults);
            await _context.SaveChangesAsync();
            return defaults;
        }

        // Update job counts
        foreach (var cat in categories)
        {
            cat.JobCount = await _context.Jobs.CountAsync(j => j.Category == cat.Name && j.Status == "Open");
        }
        return categories;
    }

    // ── Stats ─────────────────────────────────────────────────
    public async Task<object> GetPlatformStatsAsync()
    {
        return new
        {
            totalJobs = await _context.Jobs.CountAsync(),
            openJobs = await _context.Jobs.CountAsync(j => j.Status == "Open"),
            totalFreelancers = await _context.UserProfiles.CountAsync(),
            activeContracts = await _context.Contracts.CountAsync(c => c.Status == "Active"),
            completedContracts = await _context.Contracts.CountAsync(c => c.Status == "Completed"),
            totalVolume = await _context.Contracts.Where(c => c.Status == "Completed").SumAsync(c => c.AgreedAmount),
            totalApplications = await _context.JobApplications.CountAsync(),
            totalReviews = await _context.Reviews.CountAsync()
        };
    }
}
