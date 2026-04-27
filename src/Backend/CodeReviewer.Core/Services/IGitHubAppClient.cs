using CodeReviewer.Core.Models;

namespace CodeReviewer.Core.Services;

public interface IGitHubAppClient
{
    Task<PullRequestContext> GetPullRequestContextAsync(
        long installationId, string owner, string repo, int prNumber, CancellationToken ct = default);

    Task PostReviewAsync(
        long installationId, string owner, string repo, int prNumber, string commitSha,
        AiReviewResult review, CancellationToken ct = default);
}
