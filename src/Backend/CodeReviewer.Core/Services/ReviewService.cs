using System.Text.Json;
using CodeReviewer.Core.Entities;
using CodeReviewer.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeReviewer.Core.Services;

public class ReviewService : IReviewService
{
    private readonly IGitHubAppClient _github;
    private readonly IGroqClient _groq;
    private readonly IReviewRepository _repo;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IGitHubAppClient github,
        IGroqClient groq,
        IReviewRepository repo,
        ILogger<ReviewService> logger)
    {
        _github = github;
        _groq = groq;
        _repo = repo;
        _logger = logger;
    }

    public async Task ReviewAsync(ReviewRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Reviewing {Owner}/{Repo}#{Pr} ({Sha})",
            request.RepoOwner, request.RepoName, request.PrNumber, request.HeadSha);

        var prContext = await _github.GetPullRequestContextAsync(
            request.InstallationId, request.RepoOwner, request.RepoName, request.PrNumber, ct);

        var aiResult = await _groq.ReviewAsync(prContext, ct);

        await _github.PostReviewAsync(
            request.InstallationId, request.RepoOwner, request.RepoName, request.PrNumber,
            request.HeadSha, aiResult, ct);

        await _repo.AddAsync(new Review
        {
            RepoOwner = request.RepoOwner,
            RepoName = request.RepoName,
            PrNumber = request.PrNumber,
            HeadSha = request.HeadSha,
            PrTitle = prContext.Title,
            PrUrl = prContext.Url,
            Score = aiResult.Score,
            Summary = aiResult.Summary,
            CommentCount = aiResult.Comments.Count,
            RawJson = JsonSerializer.Serialize(aiResult),
            CreatedAt = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("Review posted for {Owner}/{Repo}#{Pr}, score {Score}",
            request.RepoOwner, request.RepoName, request.PrNumber, aiResult.Score);
    }
}
