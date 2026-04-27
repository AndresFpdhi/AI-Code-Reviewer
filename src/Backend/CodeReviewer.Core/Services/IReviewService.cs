using CodeReviewer.Core.Models;

namespace CodeReviewer.Core.Services;

public interface IReviewService
{
    Task ReviewAsync(ReviewRequest request, CancellationToken ct = default);
}
