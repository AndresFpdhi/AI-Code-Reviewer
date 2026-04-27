using CodeReviewer.Core.Entities;
using CodeReviewer.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CodeReviewer.Tests;

public class ReviewRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ReviewRepository _repo;

    public ReviewRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new ReviewRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Review NewReview(int prNumber, DateTime? createdAt = null) => new()
    {
        RepoOwner = "owner",
        RepoName = "repo",
        PrNumber = prNumber,
        HeadSha = "sha",
        PrTitle = $"PR {prNumber}",
        PrUrl = $"https://gh/x/{prNumber}",
        Score = 7,
        Summary = "summary",
        RawJson = "{}",
        CommentCount = 0,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    [Fact]
    public async Task Add_and_count_round_trips()
    {
        await _repo.AddAsync(NewReview(1));
        await _repo.AddAsync(NewReview(2));

        (await _repo.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task List_returns_newest_first()
    {
        await _repo.AddAsync(NewReview(1, DateTime.UtcNow.AddHours(-2)));
        await _repo.AddAsync(NewReview(2, DateTime.UtcNow.AddHours(-1)));
        await _repo.AddAsync(NewReview(3, DateTime.UtcNow));

        var page = await _repo.ListAsync(0, 10);

        page.Select(r => r.PrNumber).Should().Equal(3, 2, 1);
    }

    [Fact]
    public async Task List_honors_skip_and_take()
    {
        for (var i = 1; i <= 5; i++)
            await _repo.AddAsync(NewReview(i, DateTime.UtcNow.AddMinutes(-i)));

        var page = await _repo.ListAsync(skip: 2, take: 2);

        page.Should().HaveCount(2);
        page.Select(r => r.PrNumber).Should().Equal(3, 4);
    }

    [Fact]
    public async Task Get_returns_null_for_missing_id()
    {
        (await _repo.GetAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task Get_returns_review_by_id()
    {
        await _repo.AddAsync(NewReview(7));
        var saved = (await _repo.ListAsync(0, 1))[0];

        var fetched = await _repo.GetAsync(saved.Id);

        fetched.Should().NotBeNull();
        fetched!.PrNumber.Should().Be(7);
    }
}
