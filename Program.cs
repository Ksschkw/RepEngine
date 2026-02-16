using RepEngine.Services;
using RepEngine.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<FairScoreService>();
builder.Services.AddScoped<ReputationService>();
builder.Services.AddScoped<GovernanceService>();
builder.Services.AddScoped<JobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// ===== API Endpoints =====

// FairScore API
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

// Dashboard Data API
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

// Governance APIs
app.MapGet("/api/governance/proposals", (GovernanceService service) =>
{
    var proposals = service.GetAllProposals();
    return Results.Json(proposals);
});

app.MapGet("/api/governance/proposals/active", (GovernanceService service) =>
{
    var proposals = service.GetActiveProposals();
    return Results.Json(proposals);
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
        var vote = await service.CastVoteAsync(request.ProposalId, request.VoterWallet, request.InFavor);
        return Results.Json(vote);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/governance/proposals/{id:int}/votes", (int id, GovernanceService service) =>
{
    var votes = service.GetProposalVotes(id);
    return Results.Json(votes);
});

app.MapGet("/api/governance/has-voted", (int proposalId, string wallet, GovernanceService service) =>
{
    var hasVoted = service.HasUserVoted(proposalId, wallet);
    return Results.Json(new { hasVoted });
});

// Job APIs
app.MapGet("/api/jobs", async (string wallet, JobService service) =>
{
    var jobs = await service.GetAvailableJobsAsync(wallet);
    return Results.Json(jobs);
});

app.MapGet("/api/jobs/all", (JobService service) =>
{
    var jobs = service.GetAllJobs();
    return Results.Json(jobs);
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
    var applications = service.GetJobApplications(id);
    return Results.Json(applications);
});

// Reputation Tiers API
app.MapGet("/api/tiers", (ReputationService service) =>
{
    var tiers = service.GetAllTiers();
    return Results.Json(tiers);
});

app.Run();

// Request DTOs
record CreateProposalRequest(string CreatorWallet, string Title, string Description, string Category, int VotingDurationDays);
record VoteRequest(int ProposalId, string VoterWallet, bool InFavor);
record JobApplicationRequest(int JobId, string FreelancerWallet, string CoverLetter, decimal ProposedRate);

