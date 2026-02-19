using RepEngine.Services;
using RepEngine.Models;
using System.Text.Json;

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

// ── Services ───────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FairScoreService>();
builder.Services.AddScoped<ReputationService>();
builder.Services.AddScoped<GovernanceService>();
builder.Services.AddScoped<JobService>();

var app = builder.Build();

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
//  Swagger / OpenAPI  (no NuGet dependency — handcrafted)
// ═══════════════════════════════════════════════════════════

app.MapGet("/swagger/v1/swagger.json", () =>
{
    var spec = new
    {
        openapi = "3.0.3",
        info = new
        {
            title = "RepEngine API",
            version = "1.0.0",
            description = "Reputation-powered DAO governance & freelance marketplace — built on FairScale."
        },
        paths = new Dictionary<string, object>
        {
            ["/api/fairscore"] = new
            {
                get = new
                {
                    tags = new[] { "FairScore" },
                    summary = "Get FairScore for a wallet",
                    parameters = new[] { new { name = "wallet", @in = "query", required = true, schema = new { type = "string" }, description = "Solana wallet address" } },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Score data" }, ["400"] = new { description = "Error" } }
                }
            },
            ["/api/dashboard"] = new
            {
                get = new
                {
                    tags = new[] { "Dashboard" },
                    summary = "Get dashboard data (tier, voting power, suggestions)",
                    parameters = new[] { new { name = "wallet", @in = "query", required = true, schema = new { type = "string" } } },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Dashboard data" } }
                }
            },
            ["/api/governance/proposals"] = new
            {
                get = new
                {
                    tags = new[] { "Governance" },
                    summary = "Get all proposals",
                    responses = new Dictionary<string, object> { ["200"] = new { description = "List of proposals" } }
                }
            },
            ["/api/governance/proposals/active"] = new
            {
                get = new
                {
                    tags = new[] { "Governance" },
                    summary = "Get active proposals only",
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Active proposals" } }
                }
            },
            ["/api/governance/vote"] = new
            {
                post = new
                {
                    tags = new[] { "Governance" },
                    summary = "Cast a vote on a proposal",
                    requestBody = new
                    {
                        required = true,
                        content = new Dictionary<string, object>
                        {
                            ["application/json"] = new
                            {
                                schema = new
                                {
                                    type = "object",
                                    properties = new Dictionary<string, object>
                                    {
                                        ["proposalId"] = new { type = "integer" },
                                        ["walletAddress"] = new { type = "string" },
                                        ["vote"] = new { type = "string", description = "For or Against" }
                                    }
                                }
                            }
                        }
                    },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Vote recorded" } }
                }
            },
            ["/api/jobs"] = new
            {
                get = new
                {
                    tags = new[] { "Jobs" },
                    summary = "Get jobs available for the wallet's reputation tier",
                    parameters = new[] { new { name = "wallet", @in = "query", required = true, schema = new { type = "string" } } },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Filtered job list" } }
                }
            },
            ["/api/jobs/all"] = new
            {
                get = new
                {
                    tags = new[] { "Jobs" },
                    summary = "Get all jobs (unfiltered)",
                    responses = new Dictionary<string, object> { ["200"] = new { description = "All jobs" } }
                }
            },
            ["/api/jobs/apply"] = new
            {
                post = new
                {
                    tags = new[] { "Jobs" },
                    summary = "Apply to a job",
                    requestBody = new
                    {
                        required = true,
                        content = new Dictionary<string, object>
                        {
                            ["application/json"] = new
                            {
                                schema = new
                                {
                                    type = "object",
                                    properties = new Dictionary<string, object>
                                    {
                                        ["jobId"] = new { type = "integer" },
                                        ["freelancerWallet"] = new { type = "string" },
                                        ["coverLetter"] = new { type = "string" },
                                        ["proposedRate"] = new { type = "number" }
                                    }
                                }
                            }
                        }
                    },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Application submitted" } }
                }
            },
            ["/api/tiers"] = new
            {
                get = new
                {
                    tags = new[] { "Reputation" },
                    summary = "Get all reputation tier definitions",
                    responses = new Dictionary<string, object> { ["200"] = new { description = "Tier list" } }
                }
            },
            ["/api/governance/has-voted"] = new
            {
                get = new
                {
                    tags = new[] { "Governance" },
                    summary = "Check if a wallet has voted on a proposal",
                    parameters = new object[]
                    {
                        new { name = "proposalId", @in = "query", required = true, schema = new { type = "integer" } },
                        new { name = "wallet",     @in = "query", required = true, schema = new { type = "string"  } }
                    },
                    responses = new Dictionary<string, object> { ["200"] = new { description = "{ hasVoted: bool }" } }
                }
            }
        }
    };
    return Results.Json(spec, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
}).ExcludeFromDescription();

// Swagger UI (CDN-hosted, custom dark theme)
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

    /* ── Header banner ── */
    .docs-header {
      background: linear-gradient(135deg, #8b5cf6 0%, #ec4899 100%);
      padding: 1.5rem 2rem;
      text-align: center;
      color: #fff;
    }
    .docs-header h1 { margin: 0; font-size: 1.5rem; font-weight: 800; }
    .docs-header p  { margin: .3rem 0 0; opacity: .85; font-size: .85rem; }
    .docs-header a  { color: #fff; text-decoration: underline; }

    /* ── Container ── */
    .swagger-ui { max-width: 1100px; margin: 0 auto; padding: 1rem; }

    /* ── Hide default topbar ── */
    .swagger-ui .topbar { display: none; }

    /* ── Dark backgrounds ── */
    .swagger-ui,
    .swagger-ui .scheme-container,
    .swagger-ui .opblock-tag,
    .swagger-ui section.models,
    .swagger-ui section.models .model-container,
    .swagger-ui .model-box,
    .swagger-ui .opblock .opblock-section-header {
      background: #0a0e1a !important;
      color: #e2e8f0 !important;
    }
    .swagger-ui .wrapper { background: transparent; }

    /* ── Info section text ── */
    .swagger-ui .info .title,
    .swagger-ui .info h2,
    .swagger-ui .info h3,
    .swagger-ui .info li,
    .swagger-ui .info p,
    .swagger-ui .info table td,
    .swagger-ui .info table th,
    .swagger-ui .renderedMarkdown p { color: #cbd5e1 !important; }
    .swagger-ui .info .title { color: #f1f5f9 !important; font-weight: 700; }
    .swagger-ui .info a { color: #a78bfa !important; }

    /* ── Tag groups ── */
    .swagger-ui .opblock-tag {
      border-bottom: 1px solid rgba(255,255,255,.06) !important;
      color: #f1f5f9 !important;
      font-weight: 600;
    }
    .swagger-ui .opblock-tag:hover { background: rgba(139,92,246,.06) !important; }
    .swagger-ui .opblock-tag small { color: #94a3b8 !important; }

    /* ── Operation blocks ── */
    .swagger-ui .opblock { border: 1px solid rgba(255,255,255,.08) !important; border-radius: 10px !important; margin-bottom: .75rem; }
    .swagger-ui .opblock .opblock-summary { padding: .6rem .85rem; border-radius: 10px; }
    .swagger-ui .opblock.opblock-get    { background: rgba(59,130,246,.08)  !important; border-color: rgba(59,130,246,.2) !important; }
    .swagger-ui .opblock.opblock-post   { background: rgba(16,185,129,.08) !important; border-color: rgba(16,185,129,.2) !important; }
    .swagger-ui .opblock.opblock-put    { background: rgba(245,158,11,.08) !important; border-color: rgba(245,158,11,.2) !important; }
    .swagger-ui .opblock.opblock-delete { background: rgba(239,68,68,.08)  !important; border-color: rgba(239,68,68,.2) !important; }

    .swagger-ui .opblock .opblock-summary-method {
      border-radius: 6px !important;
      font-weight: 700;
      font-size: .75rem;
      min-width: 60px;
    }
    .swagger-ui .opblock .opblock-summary-path,
    .swagger-ui .opblock .opblock-summary-description { color: #e2e8f0 !important; }
    .swagger-ui .opblock .opblock-summary-path { font-weight: 600; }

    /* ── Expanded operation body ── */
    .swagger-ui .opblock-body { background: #111827 !important; }
    .swagger-ui .opblock .opblock-section-header { background: #1f2937 !important; border-bottom: 1px solid rgba(255,255,255,.06); }
    .swagger-ui .opblock .opblock-section-header h4 { color: #f1f5f9 !important; }

    /* ── Tables ── */
    .swagger-ui table thead tr td, .swagger-ui table thead tr th { color: #94a3b8 !important; border-bottom: 1px solid rgba(255,255,255,.08) !important; }
    .swagger-ui table tbody tr td { color: #cbd5e1 !important; border-bottom: 1px solid rgba(255,255,255,.04) !important; }
    .swagger-ui .parameter__name { color: #e2e8f0 !important; }
    .swagger-ui .parameter__type { color: #a78bfa !important; }
    .swagger-ui .parameter__in   { color: #94a3b8 !important; }

    /* ── Input fields ── */
    .swagger-ui input[type=text],
    .swagger-ui textarea,
    .swagger-ui select {
      background: #1f2937 !important; color: #f1f5f9 !important;
      border: 1px solid rgba(255,255,255,.12) !important; border-radius: 6px !important;
    }
    .swagger-ui input[type=text]:focus,
    .swagger-ui textarea:focus { border-color: #8b5cf6 !important; outline: none; box-shadow: 0 0 0 2px rgba(139,92,246,.2); }
    .swagger-ui select { background-image: none; }

    /* ── Buttons ── */
    .swagger-ui .btn { border-radius: 6px !important; font-weight: 600; }
    .swagger-ui .btn.execute { background: #8b5cf6 !important; border-color: #8b5cf6 !important; color: #fff !important; }
    .swagger-ui .btn.execute:hover { background: #7c3aed !important; }
    .swagger-ui .btn.cancel { background: transparent !important; color: #f87171 !important; border-color: #f87171 !important; }

    /* ── Response section ── */
    .swagger-ui .responses-inner { background: #111827 !important; }
    .swagger-ui .responses-table .response-col_status { color: #34d399 !important; font-weight: 600; }
    .swagger-ui .responses-table .response-col_description__inner p { color: #94a3b8 !important; }
    .swagger-ui .response-col_links { color: #94a3b8 !important; }

    /* ── Code / JSON blocks ── */
    .swagger-ui .highlight-code,
    .swagger-ui .microlight,
    .swagger-ui pre { background: #0f172a !important; color: #e2e8f0 !important; border-radius: 8px !important; }
    .swagger-ui .copy-to-clipboard { background: #1f2937 !important; border: none !important; }

    /* ── Models section ── */
    .swagger-ui section.models { border: 1px solid rgba(255,255,255,.06) !important; border-radius: 10px !important; }
    .swagger-ui section.models h4 { color: #f1f5f9 !important; border: none !important; }
    .swagger-ui .model-title { color: #a78bfa !important; }
    .swagger-ui .model { color: #cbd5e1 !important; }
    .swagger-ui .prop-type { color: #818cf8 !important; }
    .swagger-ui .model .property.primitive { color: #94a3b8 !important; }

    /* ── Misc ── */
    .swagger-ui .loading-container .loading::after { color: #a78bfa; }
    .swagger-ui svg.arrow { fill: #94a3b8 !important; }
    .swagger-ui .expand-operation svg { fill: #94a3b8 !important; }
    .swagger-ui .scheme-container { border-bottom: 1px solid rgba(255,255,255,.06) !important; padding: 1rem 0; }
    .swagger-ui .servers-title, .swagger-ui .servers label { color: #cbd5e1 !important; }
    .swagger-ui .response-control-media-type__title { color: #94a3b8 !important; }
  </style>
</head>
<body>
  <div class="docs-header">
    <h1>⚡ RepEngine API</h1>
    <p>Reputation-powered DAO governance & job marketplace &mdash; <a href="/">Back to App</a></p>
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

// ── Governance ─────────────────────────────────────────────
app.MapGet("/api/governance/proposals", (GovernanceService service) =>
{
    return Results.Json(service.GetAllProposals());
});

app.MapGet("/api/governance/proposals/active", (GovernanceService service) =>
{
    return Results.Json(service.GetActiveProposals());
});

app.MapGet("/api/governance/proposals/{id:int}", (int id, GovernanceService service) =>
{
    var proposal = service.GetProposal(id);
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
            request.VotingDurationDays
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

app.MapGet("/api/governance/proposals/{id:int}/votes", (int id, GovernanceService service) =>
{
    return Results.Json(service.GetProposalVotes(id));
});

app.MapGet("/api/governance/has-voted", (int proposalId, string wallet, GovernanceService service) =>
{
    var hasVoted = service.HasUserVoted(proposalId, wallet);
    return Results.Json(new { hasVoted });
});

// ── Jobs ───────────────────────────────────────────────────
app.MapGet("/api/jobs", async (string wallet, JobService service) =>
{
    var jobs = await service.GetAvailableJobsAsync(wallet);
    return Results.Json(jobs);
});

app.MapGet("/api/jobs/all", (JobService service) =>
{
    return Results.Json(service.GetAllJobs());
});

app.MapGet("/api/jobs/{id:int}", (int id, JobService service) =>
{
    var job = service.GetJob(id);
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
            request.ProposedRate
        );
        return Results.Json(application);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/jobs/{id:int}/applications", (int id, JobService service) =>
{
    return Results.Json(service.GetJobApplications(id));
});

// ── Reputation Tiers ───────────────────────────────────────
app.MapGet("/api/tiers", (ReputationService service) =>
{
    return Results.Json(service.GetAllTiers());
});

app.Run();

// ── Request DTOs ───────────────────────────────────────────
record CreateProposalRequest(string CreatorWallet, string Title, string Description, string Category, int VotingDurationDays = 7);
record VoteRequest(int ProposalId, string WalletAddress, string? Vote = null, bool? InFavor = null);
record JobApplicationRequest(int JobId, string FreelancerWallet, string CoverLetter, decimal ProposedRate);
