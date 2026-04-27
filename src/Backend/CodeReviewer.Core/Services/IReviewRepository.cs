using CodeReviewer.Core.Entities;

namespace CodeReviewer.Core.Services;

public interface IReviewRepository
{
    Task AddAsync(Review review, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> ListAsync(int skip, int take, CancellationToken ct = default);
    Task<Review?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
