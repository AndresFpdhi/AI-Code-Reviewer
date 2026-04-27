using CodeReviewer.Core.Models;

namespace CodeReviewer.Core.Services;

public interface IGroqClient
{
    Task<AiReviewResult> ReviewAsync(PullRequestContext context, CancellationToken ct = default);
}
