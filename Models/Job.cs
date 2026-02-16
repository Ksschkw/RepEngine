namespace RepEngine.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string MinimumTierRequired { get; set; } = "Bronze";
        public string ClientWallet { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; }
        public string Status { get; set; } = "Open";
        public int ApplicationCount { get; set; }
        public DateTime? Deadline { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public bool IsPremium { get; set; }
    }

    public class JobApplication
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string FreelancerWallet { get; set; } = string.Empty;
        public string CoverLetter { get; set; } = string.Empty;
        public decimal ProposedRate { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
