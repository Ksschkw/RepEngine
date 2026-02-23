using RepEngine.Services;
using RepEngine.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

// Load .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────
builder.Services.AddDbContext<RepEngine.Data.RepEngineContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ───────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FairScoreService>();
builder.Services.AddScoped<ReputationService>();
builder.Services.AddScoped<GovernanceService>();
builder.Services.AddScoped<JobService>();

var app = builder.Build();

// ── Auto-migrate on startup (dev convenience) ──────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RepEngine.Data.RepEngineContext>();
    db.Database.Migrate();
}

// ── Middleware ──────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// ═══════════════════════════════════════════════════════════
//  Swagger / OpenAPI
// ═══════════════════════════════════════════════════════════

app.MapGet("/swagger/v1/swagger.json", () =>
{
    var spec = new
    {
        openapi = "3.0.3",
        info = new
        {
            title = "RepEngine API",
            version = "2.0.0",
            description = "Enterprise-grade reputation-powered gig marketplace & DAO governance — built on FairScale."
        },
        paths = new Dictionary<string, object>
        {
            ["/api/fairscore"] = new { get = new { tags = SwaggerTags.FairScore, summary = "Get FairScore for a wallet" } },
            ["/api/dashboard"] = new { get = new { tags = SwaggerTags.Dashboard, summary = "Get dashboard data" } },
            ["/api/profile"] = new { get = new { tags = SwaggerTags.Profile, summary = "Get user profile" } },
            ["/api/profile/update"] = new { post = new { tags = SwaggerTags.Profile, summary = "Update user profile" } },
            ["/api/jobs"] = new { get = new { tags = SwaggerTags.Jobs, summary = "Get jobs available for wallet" } },
            ["/api/jobs/all"] = new { get = new { tags = SwaggerTags.Jobs, summary = "Get all jobs" } },
            ["/api/jobs/search"] = new { get = new { tags = SwaggerTags.Jobs, summary = "Search/filter jobs" } },
            ["/api/jobs/apply"] = new { post = new { tags = SwaggerTags.Jobs, summary = "Apply to a job" } },
            ["/api/jobs/create"] = new { post = new { tags = SwaggerTags.Jobs, summary = "Create a job" } },
            ["/api/jobs/categories"] = new { get = new { tags = SwaggerTags.Jobs, summary = "Get job categories" } },
            ["/api/contracts"] = new { get = new { tags = SwaggerTags.Contracts, summary = "Get user contracts" } },
            ["/api/reviews"] = new { get = new { tags = SwaggerTags.Reviews, summary = "Get user reviews" } },
            ["/api/notifications"] = new { get = new { tags = SwaggerTags.Notifications, summary = "Get notifications" } },
            ["/api/governance/proposals"] = new { get = new { tags = SwaggerTags.Governance, summary = "Get proposals" } },
            ["/api/governance/vote"] = new { post = new { tags = SwaggerTags.Governance, summary = "Cast a vote" } },
            ["/api/governance/stats"] = new { get = new { tags = SwaggerTags.Governance, summary = "Get governance stats" } },
            ["/api/stats"] = new { get = new { tags = SwaggerTags.Stats, summary = "Platform statistics" } },
        }
    };
    return Results.Json(spec, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
}).ExcludeFromDescription();

// Swagger UI
app.MapGet("/docs", () => Results.Content("""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>RepEngine API Docs</title>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap"/>
  <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"/>
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; background: #0a0e1a; font-family: 'Inter', sans-serif; }
    .docs-header { background: linear-gradient(135deg, #8b5cf6 0%, #ec4899 100%); padding: 1.5rem 2rem; text-align: center; color: #fff; }
    .docs-header h1 { margin: 0; font-size: 1.5rem; font-weight: 800; }
    .docs-header p  { margin: .3rem 0 0; opacity: .85; font-size: .85rem; }
    .docs-header a  { color: #fff; text-decoration: underline; }
    .swagger-ui { max-width: 1100px; margin: 0 auto; padding: 1rem; }
    .swagger-ui .topbar { display: none; }
    .swagger-ui, .swagger-ui .scheme-container, .swagger-ui .opblock-tag,
    .swagger-ui section.models, .swagger-ui section.models .model-container,
    .swagger-ui .model-box, .swagger-ui .opblock .opblock-section-header { background: #0a0e1a !important; color: #e2e8f0 !important; }
    .swagger-ui .wrapper { background: transparent; }
    .swagger-ui .info .title, .swagger-ui .info p, .swagger-ui .renderedMarkdown p { color: #cbd5e1 !important; }
    .swagger-ui .info .title { color: #f1f5f9 !important; font-weight: 700; }
    .swagger-ui .opblock-tag { border-bottom: 1px solid rgba(255,255,255,.06) !important; color: #f1f5f9 !important; font-weight: 600; }
    .swagger-ui .opblock { border: 1px solid rgba(255,255,255,.08) !important; border-radius: 10px !important; margin-bottom: .75rem; }
    .swagger-ui .opblock.opblock-get    { background: rgba(59,130,246,.08) !important; }
    .swagger-ui .opblock.opblock-post   { background: rgba(16,185,129,.08) !important; }
    .swagger-ui .opblock.opblock-put    { background: rgba(245,158,11,.08) !important; }
    .swagger-ui .opblock.opblock-delete { background: rgba(239,68,68,.08) !important; }
    .swagger-ui input[type=text], .swagger-ui textarea, .swagger-ui select { background: #1f2937 !important; color: #f1f5f9 !important; border: 1px solid rgba(255,255,255,.12) !important; border-radius: 6px !important; }
    .swagger-ui .btn.execute { background: #8b5cf6 !important; border-color: #8b5cf6 !important; color: #fff !important; }
    .swagger-ui .highlight-code, .swagger-ui .microlight, .swagger-ui pre { background: #0f172a !important; color: #e2e8f0 !important; border-radius: 8px !important; }
    .swagger-ui table thead tr td, .swagger-ui table thead tr th { color: #94a3b8 !important; }
    .swagger-ui table tbody tr td { color: #cbd5e1 !important; }
  </style>
</head>
<body>
  <div class="docs-header">
    <h1>⚡ RepEngine API v2.0</h1>
    <p>Enterprise reputation-powered gig marketplace &mdash; <a href="/">Back to App</a></p>
  </div>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
  <script>
    SwaggerUIBundle({
      url: '/swagger/v1/swagger.json',
      dom_id: '#swagger-ui',
      deepLinking: true,
      presets: [SwaggerUIBundle.presets.apis, SwaggerUIBundle.SwaggerUIStandalonePreset],
      layout: 'BaseLayout',
      defaultModelsExpandDepth: -1
    });
  </script>
</body>
</html>
""", "text/html")).ExcludeFromDescription();


// ═══════════════════════════════════════════════════════════
//  API Endpoints
// ═══════════════════════════════════════════════════════════

// ── FairScore ──────────────────────────────────────────────
app.MapGet("/api/fairscore", async (string wallet, FairScoreService service) =>
{
    try
    {
        var scoreData = await service.GetScoreAsync(wallet);
        return Results.Json(scoreData);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ── Dashboard ──────────────────────────────────────────────
app.MapGet("/api/dashboard", async (string wallet, ReputationService service) =>
{
    try
    {
        var data = await service.GetUserDashboardDataAsync(wallet);
        return Results.Json(data);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ── User Profile ───────────────────────────────────────────
app.MapGet("/api/profile", async (string wallet, JobService service, FairScoreService fs) =>
{
    try
    {
        var profile = await service.GetUserProfileAsync(wallet);
        if (profile == null) return Results.NotFound();

        // Enrich with live FairScore
        try
        {
            var score = await fs.GetScoreAsync(wallet);
            profile.CurrentScore = score.Score;
            profile.CurrentTier = Enum.TryParse<TierLevel>(score.Tier, true, out var t) ? t : TierLevel.Unranked;
        }
        catch { }

        var reviews = await service.GetUserReviewsAsync(wallet);
        return Results.Json(new { profile, reviews });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/profile/update", async (ProfileUpdateRequest request, JobService service) =>
{
    try
    {
        var profile = await service.UpdateUserProfileAsync(request.WalletAddress, new UserProfile
        {
            DisplayName = request.DisplayName ?? "",
            Bio = request.Bio ?? "",
            Title = request.Title ?? "",
            AvatarUrl = request.AvatarUrl ?? "",
            Skills = request.Skills ?? new(),
            Languages = request.Languages ?? new(),
            PortfolioUrl = request.PortfolioUrl ?? "",
            GithubUrl = request.GithubUrl ?? "",
            TwitterUrl = request.TwitterUrl ?? "",
            WebsiteUrl = request.WebsiteUrl ?? "",
            HourlyRate = request.HourlyRate,
            AvailabilityStatus = request.AvailabilityStatus ?? "Available",
            Timezone = request.Timezone ?? "UTC"
        });
        return Results.Json(profile);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ── Governance ─────────────────────────────────────────────
app.MapGet("/api/governance/proposals", async (GovernanceService service) =>
{
    return Results.Json(await service.GetAllProposalsAsync());
});

app.MapGet("/api/governance/proposals/active", async (GovernanceService service) =>
{
    return Results.Json(await service.GetActiveProposalsAsync());
});

app.MapGet("/api/governance/proposals/{id:int}", async (int id, GovernanceService service) =>
{
    var proposal = await service.GetProposalAsync(id);
    return proposal != null ? Results.Json(proposal) : Results.NotFound();
});

app.MapPost("/api/governance/proposals", async (CreateProposalRequest request, GovernanceService service) =>
{
    try
    {
        var proposal = await service.CreateProposalAsync(
            request.CreatorWallet,
            request.Title,
            request.Description,
            request.Category,
            request.VotingDurationDays,
            request.MinimumTierToVote ?? "Unranked",
            request.QuorumRequired
        );
        return Results.Json(proposal);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/governance/vote", async (VoteRequest request, GovernanceService service) =>
{
    try
    {
        bool inFavor = request.InFavor
            ?? (request.Vote?.Equals("For", StringComparison.OrdinalIgnoreCase) ?? true);
        var vote = await service.CastVoteAsync(request.ProposalId, request.WalletAddress, inFavor);
        return Results.Json(vote);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/governance/proposals/{id:int}/votes", async (int id, GovernanceService service) =>
{
    return Results.Json(await service.GetProposalVotesAsync(id));
});

app.MapGet("/api/governance/has-voted", async (int proposalId, string wallet, GovernanceService service) =>
{
    var hasVoted = await service.HasUserVotedAsync(proposalId, wallet);
    return Results.Json(new { hasVoted });
});

app.MapGet("/api/governance/stats", async (GovernanceService service) =>
{
    return Results.Json(await service.GetGovernanceStatsAsync());
});

// ── Jobs ───────────────────────────────────────────────────
app.MapGet("/api/jobs", async (string wallet, JobService service) =>
{
    var jobs = await service.GetAvailableJobsAsync(wallet);
    return Results.Json(jobs);
});

app.MapGet("/api/jobs/all", async (JobService service) =>
{
    return Results.Json(await service.GetAllJobsAsync());
});

app.MapGet("/api/jobs/search", async (string? q, string? category, string? tier, string? budgetType, string? experienceLevel, JobService service) =>
{
    return Results.Json(await service.SearchJobsAsync(q, category, tier, budgetType, experienceLevel));
});

app.MapGet("/api/jobs/{id:int}", async (int id, JobService service) =>
{
    var job = await service.GetJobAsync(id);
    return job != null ? Results.Json(job) : Results.NotFound();
});

app.MapPost("/api/jobs/apply", async (JobApplicationRequest request, JobService service) =>
{
    try
    {
        var application = await service.ApplyToJobAsync(
            request.JobId,
            request.FreelancerWallet,
            request.CoverLetter,
            request.ProposedRate,
            request.DeliveryDays,
            request.PortfolioLinks ?? "",
            request.Availability ?? "Immediate"
        );
        return Results.Json(application);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/jobs/create", async (CreateJobRequest request, JobService service) =>
{
    try
    {
        var job = new Job
        {
            Title = request.Title,
            Description = request.Description,
            Budget = request.Budget,
            BudgetType = request.BudgetType ?? "Fixed",
            PaymentCurrency = request.PaymentCurrency ?? "USDC",
            Category = request.Category ?? "Development",
            ExperienceLevel = request.ExperienceLevel ?? "Intermediate",
            ProjectLength = request.ProjectLength ?? "Short",
            MinimumTierRequired = request.MinimumTierRequired,
            MinimumFairScore = request.MinimumFairScore,
            Visibility = request.Visibility ?? "Public",
            ClientWallet = request.ClientWallet,
            Skills = request.Skills ?? [],
            Tags = request.Tags ?? [],
            Location = request.Location ?? "Remote",
            IsPremium = request.IsPremium,
            Deadline = request.Deadline
        };
        var created = await service.CreateJobAsync(job);
        return Results.Json(created);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/jobs/{id:int}/applications", async (int id, JobService service, FairScoreService fs) =>
{
    var apps = await service.GetJobApplicationsAsync(id);
    var results = new List<object>();
    foreach(var a in apps)
    {
        // Use snapshot score first, live score as enrichment
        var tierName = a.TierAtApplication;
        var fairScore = a.FairScoreAtApplication;
        try
        {
            var scoreData = await fs.GetScoreAsync(a.FreelancerWallet);
            tierName = scoreData.Tier ?? tierName;
            fairScore = scoreData.Score;
        }
        catch { }

        results.Add(new {
            id = a.Id,
            jobId = a.JobId,
            freelancerWallet = a.FreelancerWallet,
            coverLetter = a.CoverLetter,
            proposedRate = a.ProposedRate,
            deliveryDays = a.DeliveryDays,
            portfolioLinks = a.PortfolioLinks,
            availability = a.Availability,
            fairScoreAtApplication = a.FairScoreAtApplication,
            currentFairScore = fairScore,
            appliedAt = a.AppliedAt,
            status = a.Status,
            freelancerTier = tierName
        });
    }
    return Results.Json(results);
});

app.MapPut("/api/jobs/applications/{id:int}/approve", async (int id, ApproveApplicationRequest request, JobService service) =>
{
    try
    {
        var contract = await service.ApproveApplicationAsync(id, request.ClientWallet);
        return Results.Json(contract);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/jobs/applications/{id:int}/shortlist", async (int id, ApproveApplicationRequest request, JobService service) =>
{
    try
    {
        var app = await service.ShortlistApplicationAsync(id, request.ClientWallet);
        return Results.Json(app);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPut("/api/jobs/applications/{id:int}/reject", async (int id, ApproveApplicationRequest request, JobService service) =>
{
    try
    {
        var app = await service.RejectApplicationAsync(id, request.ClientWallet);
        return Results.Json(app);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/jobs/applications", async (string wallet, JobService service) =>
{
    return Results.Json(await service.GetFreelancerApplicationsAsync(wallet));
});

app.MapGet("/api/jobs/categories", async (JobService service) =>
{
    return Results.Json(await service.GetCategoriesAsync());
});

// ── Saved Jobs ─────────────────────────────────────────────
app.MapPost("/api/jobs/{id:int}/save", async (int id, SaveJobRequest request, JobService service) =>
{
    try
    {
        await service.SaveJobAsync(id, request.WalletAddress);
        return Results.Json(new { saved = true });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapDelete("/api/jobs/{id:int}/save", async (int id, string wallet, JobService service) =>
{
    await service.UnsaveJobAsync(id, wallet);
    return Results.Json(new { saved = false });
});

app.MapGet("/api/jobs/saved", async (string wallet, JobService service) =>
{
    return Results.Json(await service.GetSavedJobsAsync(wallet));
});

// ── Contracts ──────────────────────────────────────────────
app.MapGet("/api/contracts", async (string wallet, JobService service) =>
{
    return Results.Json(await service.GetUserContractsAsync(wallet));
});

app.MapGet("/api/contracts/{id:int}", async (int id, JobService service) =>
{
    var contract = await service.GetContractAsync(id);
    return contract != null ? Results.Json(contract) : Results.NotFound();
});

app.MapPut("/api/contracts/{id:int}/complete", async (int id, ApproveApplicationRequest request, JobService service) =>
{
    try
    {
        var contract = await service.CompleteContractAsync(id, request.ClientWallet);
        return Results.Json(contract);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ── Reviews ────────────────────────────────────────────────
app.MapPost("/api/reviews", async (CreateReviewRequest request, JobService service) =>
{
    try
    {
        var review = await service.CreateReviewAsync(
            request.ContractId, request.ReviewerWallet,
            request.Rating, request.Comment,
            request.Communication, request.Quality,
            request.Timeliness, request.Professionalism
        );
        return Results.Json(review);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/reviews", async (string wallet, JobService service) =>
{
    return Results.Json(await service.GetUserReviewsAsync(wallet));
});

// ── Disputes ───────────────────────────────────────────────
app.MapPost("/api/disputes", async (CreateDisputeRequest request, JobService service) =>
{
    try
    {
        var dispute = await service.CreateDisputeAsync(
            request.ContractId, request.InitiatorWallet,
            request.Reason, request.Evidence
        );
        return Results.Json(dispute);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ── Messages ───────────────────────────────────────────────
app.MapPost("/api/messages", async (SendMessageRequest request, JobService service) =>
{
    try
    {
        var msg = await service.SendMessageAsync(request.SenderWallet, request.ReceiverWallet, request.Content, request.ContractId);
        return Results.Json(msg);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/messages", async (string wallet, string otherWallet, int? contractId, JobService service) =>
{
    return Results.Json(await service.GetConversationAsync(wallet, otherWallet, contractId));
});

// ── Notifications ──────────────────────────────────────────
app.MapGet("/api/notifications", async (string wallet, bool? unreadOnly, JobService service) =>
{
    return Results.Json(await service.GetNotificationsAsync(wallet, unreadOnly ?? false));
});

app.MapPut("/api/notifications/read", async (MarkReadRequest request, JobService service) =>
{
    await service.MarkNotificationsReadAsync(request.WalletAddress);
    return Results.Json(new { success = true });
});

// ── Reputation Tiers ───────────────────────────────────────
app.MapGet("/api/tiers", (ReputationService service) =>
{
    return Results.Json(service.GetAllTiers());
});

// ── Platform Stats ─────────────────────────────────────────
app.MapGet("/api/stats", async (JobService service) =>
{
    return Results.Json(await service.GetPlatformStatsAsync());
});

app.Run();

// ── Request DTOs ───────────────────────────────────────────
record CreateProposalRequest(string CreatorWallet, string Title, string Description, string Category, int VotingDurationDays = 7, string? MinimumTierToVote = "Unranked", int QuorumRequired = 100);
record VoteRequest(int ProposalId, string WalletAddress, string? Vote = null, bool? InFavor = null);
record JobApplicationRequest(int JobId, string FreelancerWallet, string CoverLetter, decimal ProposedRate, int DeliveryDays, string? PortfolioLinks = null, string? Availability = null);
record CreateJobRequest(string Title, string Description, decimal Budget, string MinimumTierRequired, string ClientWallet, List<string>? Skills, string? BudgetType = "Fixed", string? PaymentCurrency = "USDC", string? Category = "Development", string? ExperienceLevel = "Intermediate", string? ProjectLength = "Short", int MinimumFairScore = 0, string? Visibility = "Public", List<string>? Tags = null, string? Location = "Remote", bool IsPremium = false, DateTime? Deadline = null);
record ApproveApplicationRequest(string ClientWallet);
record ProfileUpdateRequest(string WalletAddress, string? DisplayName, string? Bio, string? Title, string? AvatarUrl, List<string>? Skills, List<string>? Languages, string? PortfolioUrl, string? GithubUrl, string? TwitterUrl, string? WebsiteUrl, decimal HourlyRate = 0, string? AvailabilityStatus = "Available", string? Timezone = "UTC");
record CreateReviewRequest(int ContractId, string ReviewerWallet, int Rating, string Comment, int Communication = 3, int Quality = 3, int Timeliness = 3, int Professionalism = 3);
record CreateDisputeRequest(int ContractId, string InitiatorWallet, string Reason, string Evidence);
record SendMessageRequest(string SenderWallet, string ReceiverWallet, string Content, int? ContractId = null);
record SaveJobRequest(string WalletAddress);
record MarkReadRequest(string WalletAddress);

public static class SwaggerTags
{
    public static readonly string[] FairScore = ["FairScore"];
    public static readonly string[] Dashboard = ["Dashboard"];
    public static readonly string[] Governance = ["Governance"];
    public static readonly string[] Jobs = ["Jobs"];
    public static readonly string[] Reputation = ["Reputation"];
    public static readonly string[] Profile = ["Profile"];
    public static readonly string[] Contracts = ["Contracts"];
    public static readonly string[] Reviews = ["Reviews"];
    public static readonly string[] Notifications = ["Notifications"];
    public static readonly string[] Stats = ["Platform"];
}
