using CodeReviewer.Core.Entities;
using CodeReviewer.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeReviewer.Data;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;

    public ReviewRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Review>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        return await _db.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<Review?> GetAsync(int id, CancellationToken ct = default) =>
        _db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _db.Reviews.CountAsync(ct);
}
