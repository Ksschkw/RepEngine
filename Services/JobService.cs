using RepEngine.Models;

namespace RepEngine.Services;

public class JobService
{
    private readonly ReputationService _reputationService;
    private readonly ILogger<JobService> _logger;
    private static List<Job> _jobs = new();
    private static List<JobApplication> _applications = new();
    private static int _nextJobId = 1;
    private static int _nextApplicationId = 1;

    public JobService(ReputationService reputationService, ILogger<JobService> logger)
    {
        _reputationService = reputationService;
        _logger = logger;
        InitializeSampleJobs();
    }

    public async Task<List<Job>> GetAvailableJobsAsync(string walletAddress)
    {
        var allJobs = _jobs.Where(j => j.Status == "Open").ToList();
        var accessibleJobs = new List<Job>();

        foreach (var job in allJobs)
        {
            if (await _reputationService.CanAccessJobAsync(walletAddress, job))
            {
                accessibleJobs.Add(job);
            }
        }

        return accessibleJobs.OrderByDescending(j => j.PostedAt).ToList();
    }

    public List<Job> GetAllJobs()
    {
        return _jobs.OrderByDescending(j => j.PostedAt).ToList();
    }

    public Job? GetJob(int jobId)
    {
        return _jobs.FirstOrDefault(j => j.Id == jobId);
    }

    public async Task<JobApplication> ApplyToJobAsync(int jobId, string freelancerWallet, string coverLetter, decimal proposedRate)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
            throw new ArgumentException("Job not found");

        if (job.Status != "Open")
            throw new InvalidOperationException("Job is not open for applications");

        if (!await _reputationService.CanAccessJobAsync(freelancerWallet, job))
            throw new InvalidOperationException("Your reputation tier is not high enough for this job");

        // Check if already applied
        if (_applications.Any(a => a.JobId == jobId && a.FreelancerWallet == freelancerWallet))
            throw new InvalidOperationException("You have already applied to this job");

        var application = new JobApplication
        {
            Id = _nextApplicationId++,
            JobId = jobId,
            FreelancerWallet = freelancerWallet,
            CoverLetter = coverLetter,
            ProposedRate = proposedRate,
            AppliedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        _applications.Add(application);
        job.ApplicationCount++;

        _logger.LogInformation("Application {AppId} submitted for job {JobId} by {Wallet}", 
            application.Id, jobId, freelancerWallet);

        return application;
    }

    public List<JobApplication> GetJobApplications(int jobId)
    {
        return _applications.Where(a => a.JobId == jobId).OrderByDescending(a => a.AppliedAt).ToList();
    }

    public List<JobApplication> GetFreelancerApplications(string walletAddress)
    {
        return _applications.Where(a => a.FreelancerWallet == walletAddress).OrderByDescending(a => a.AppliedAt).ToList();
    }

    private void InitializeSampleJobs()
    {
        if (_jobs.Any()) return;

        _jobs.Add(new Job
        {
            Id = _nextJobId++,
            Title = "Smart Contract Audit for DeFi Protocol",
            Description = "Need experienced Solana developer to audit our lending protocol smart contracts. Must have proven track record.",
            ClientWallet = "Client1...",
            Budget = 5000,
            MinimumTierRequired = TierLevel.Gold.ToString(),
            Status = "Open",
            PostedAt = DateTime.UtcNow.AddHours(-6),
            Deadline = DateTime.UtcNow.AddDays(14),
            Skills = new List<string> { "Solana", "Rust", "Security", "DeFi" },
            IsPremium = true,
            ApplicationCount = 3
        });

        _jobs.Add(new Job
        {
            Id = _nextJobId++,
            Title = "Frontend Developer for NFT Marketplace",
            Description = "Build responsive React frontend for NFT marketplace. Design provided. 2-week timeline.",
            ClientWallet = "Client2...",
            Budget = 2500,
            MinimumTierRequired = TierLevel.Silver.ToString(),
            Status = "Open",
            PostedAt = DateTime.UtcNow.AddHours(-12),
            Deadline = DateTime.UtcNow.AddDays(21),
            Skills = new List<string> { "React", "TypeScript", "Web3", "UI/UX" },
            IsPremium = false,
            ApplicationCount = 8
        });

        _jobs.Add(new Job
        {
            Id = _nextJobId++,
            Title = "Community Manager for DAO",
            Description = "Manage Discord and Twitter community for growing DAO. Part-time, ongoing role.",
            ClientWallet = "Client3...",
            Budget = 1500,
            MinimumTierRequired = TierLevel.Bronze.ToString(),
            Status = "Open",
            PostedAt = DateTime.UtcNow.AddHours(-2),
            Deadline = null,
            Skills = new List<string> { "Community Management", "Social Media", "DAO" },
            IsPremium = false,
            ApplicationCount = 12
        });

        _jobs.Add(new Job
        {
            Id = _nextJobId++,
            Title = "Tokenomics Design Consultant",
            Description = "Design sustainable tokenomics model for new protocol. Requires deep DeFi knowledge and modeling experience.",
            ClientWallet = "Client4...",
            Budget = 8000,
            MinimumTierRequired = TierLevel.Platinum.ToString(),
            Status = "Open",
            PostedAt = DateTime.UtcNow.AddMinutes(-30),
            Deadline = DateTime.UtcNow.AddDays(30),
            Skills = new List<string> { "Tokenomics", "DeFi", "Economics", "Modeling" },
            IsPremium = true,
            ApplicationCount = 1
        });

        _jobs.Add(new Job
        {
            Id = _nextJobId++,
            Title = "Technical Writer for Documentation",
            Description = "Create comprehensive documentation for Solana development toolkit. Must understand technical concepts.",
            ClientWallet = "Client5...",
            Budget = 1200,
            MinimumTierRequired = TierLevel.Bronze.ToString(),
            Status = "Open",
            PostedAt = DateTime.UtcNow.AddDays(-1),
            Deadline = DateTime.UtcNow.AddDays(10),
            Skills = new List<string> { "Technical Writing", "Solana", "Documentation" },
            IsPremium = false,
            ApplicationCount = 5
        });
    }
}
