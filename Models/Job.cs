using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RepEngine.Models
{
    // ── Job / Gig ──────────────────────────────────────────────
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Budget & Payment
        public decimal Budget { get; set; }
        public string BudgetType { get; set; } = "Fixed";      // Fixed | Hourly
        public string PaymentCurrency { get; set; } = "USDC";   // USDC | USDT | SOL

        // Classification
        public string Category { get; set; } = "Development";
        public string ExperienceLevel { get; set; } = "Intermediate"; // Entry | Intermediate | Expert
        public string ProjectLength { get; set; } = "Short";          // Short | Medium | Long

        // Reputation gating
        public string MinimumTierRequired { get; set; } = "Unranked";
        public int MinimumFairScore { get; set; } = 0;        // Fine-grained: require exact score
        public bool IsPremium { get; set; }

        // Visibility
        public string Visibility { get; set; } = "Public";     // Public | TierGated | Private

        // Owner
        public string ClientWallet { get; set; } = string.Empty;
        [ForeignKey("ClientWallet")]
        [JsonIgnore]
        public UserProfile? Client { get; set; }

        // Metadata
        public DateTime PostedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = "Open";           // Open | In Progress | Completed | Cancelled
        public int ApplicationCount { get; set; }

        // Rich data
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Attachments { get; set; } = new List<string>();  // URLs

        // Location
        public string Location { get; set; } = "Remote";       // Remote | On-site | Hybrid
    }

    // ── Job Application / Proposal ─────────────────────────────
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        public string FreelancerWallet { get; set; } = string.Empty;
        [ForeignKey("FreelancerWallet")]
        [JsonIgnore]
        public UserProfile? Freelancer { get; set; }

        public string CoverLetter { get; set; } = string.Empty;
        public decimal ProposedRate { get; set; }
        public int DeliveryDays { get; set; }

        // Portfolio / extra info
        public string PortfolioLinks { get; set; } = string.Empty;    // JSON array or comma-separated
        public string Availability { get; set; } = "Immediate";       // Immediate | 1 Week | 2 Weeks | 1 Month
        public DateTime? ExpectedStartDate { get; set; }

        // Snapshot of freelancer's FairScore at time of application
        public int FairScoreAtApplication { get; set; }
        public string TierAtApplication { get; set; } = "Unranked";

        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = "Pending";   // Pending | Shortlisted | Approved | Rejected | Withdrawn
    }

    // ── Contract (created when application is approved) ────────
    public class Contract
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        public int ApplicationId { get; set; }
        [ForeignKey("ApplicationId")]
        [JsonIgnore]
        public JobApplication? Application { get; set; }

        public string ClientWallet { get; set; } = string.Empty;
        public string FreelancerWallet { get; set; } = string.Empty;

        public decimal AgreedAmount { get; set; }
        public string PaymentCurrency { get; set; } = "USDC";
        public decimal PlatformFee { get; set; }           // Computed from tier discount

        public DateTime StartDate { get; set; }
        public DateTime? DeadlineDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string Status { get; set; } = "Active";    // Active | Completed | Disputed | Cancelled

        [JsonIgnore]
        public List<Milestone>? Milestones { get; set; }
    }

    // ── Milestone ──────────────────────────────────────────────
    public class Milestone
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        [JsonIgnore]
        public Contract? Contract { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderIndex { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } = "Pending";  // Pending | In Progress | Submitted | Approved | Disputed
    }

    // ── Review / Rating ────────────────────────────────────────
    public class Review
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        [JsonIgnore]
        public Contract? Contract { get; set; }

        public string ReviewerWallet { get; set; } = string.Empty;     // Who left the review
        public string RevieweeWallet { get; set; } = string.Empty;     // Who received the review
        public string ReviewerRole { get; set; } = "Client";           // Client | Freelancer

        public int Rating { get; set; }                                // 1-5
        public string Comment { get; set; } = string.Empty;
        public int Communication { get; set; }                         // 1-5 sub-rating
        public int Quality { get; set; }                               // 1-5 sub-rating
        public int Timeliness { get; set; }                            // 1-5 sub-rating
        public int Professionalism { get; set; }                       // 1-5 sub-rating

        // Snapshot of FairScore at review time
        public int ReviewerFairScore { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ── Dispute ────────────────────────────────────────────────
    public class Dispute
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        [JsonIgnore]
        public Contract? Contract { get; set; }

        public string InitiatorWallet { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;          // URL or text

        public string Status { get; set; } = "Open";                  // Open | Under Review | Resolved | Dismissed
        public string Resolution { get; set; } = string.Empty;
        public string ResolvedBy { get; set; } = string.Empty;        // Arbitrator wallet

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    // ── Message / Chat ─────────────────────────────────────────
    public class Message
    {
        public int Id { get; set; }

        public int? ContractId { get; set; }
        [ForeignKey("ContractId")]
        [JsonIgnore]
        public Contract? Contract { get; set; }

        public string SenderWallet { get; set; } = string.Empty;
        public string ReceiverWallet { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }
    }

    // ── Notification ───────────────────────────────────────────
    public class Notification
    {
        public int Id { get; set; }

        public string WalletAddress { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";                     // Info | Application | Contract | Review | Governance
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;              // Deep-link within the app
        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ── Saved / Bookmarked Jobs ────────────────────────────────
    public class SavedJob
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        public string WalletAddress { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }

    // ── Job Category ───────────────────────────────────────────
    public class JobCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "💼";
        public string Description { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }
}
