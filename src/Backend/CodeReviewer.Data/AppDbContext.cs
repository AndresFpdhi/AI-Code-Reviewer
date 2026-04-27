using CodeReviewer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeReviewer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(b =>
        {
            b.HasIndex(r => new { r.RepoOwner, r.RepoName, r.PrNumber });
            b.HasIndex(r => r.CreatedAt);
        });
    }
}
