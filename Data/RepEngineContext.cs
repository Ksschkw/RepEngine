using Microsoft.EntityFrameworkCore;
using RepEngine.Models;

namespace RepEngine.Data
{
    public class RepEngineContext : DbContext
    {
        public RepEngineContext(DbContextOptions<RepEngineContext> options) : base(options) { }

        // ── Core ──────────────────────────────────────────
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        // ── Jobs & Gigs ───────────────────────────────────
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<JobCategory> JobCategories => Set<JobCategory>();
        public DbSet<SavedJob> SavedJobs => Set<SavedJob>();

        // ── Contracts & Milestones ────────────────────────
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<Milestone> Milestones => Set<Milestone>();

        // ── Reviews & Disputes ────────────────────────────
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Dispute> Disputes => Set<Dispute>();

        // ── Communication ─────────────────────────────────
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Notification> Notifications => Set<Notification>();

        // ── Governance ────────────────────────────────────
        public DbSet<Proposal> Proposals => Set<Proposal>();
        public DbSet<Vote> Votes => Set<Vote>();
    }
}
