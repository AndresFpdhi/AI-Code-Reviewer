using CodeReviewer.Api.Controllers;
using CodeReviewer.Core.Entities;
using CodeReviewer.Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeReviewer.Tests;

public class ReviewsControllerTests
{
    private static (ReviewsController Controller, Mock<IReviewRepository> Repo) Build()
    {
        var repo = new Mock<IReviewRepository>();
        return (new ReviewsController(repo.Object), repo);
    }

    [Fact]
    public async Task List_clamps_page_to_minimum_one()
    {
        var (sut, repo) = Build();
        repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Review>());
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await sut.List(page: -5, pageSize: 20);

        repo.Verify(r => r.ListAsync(0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_clamps_page_size_to_max_100()
    {
        var (sut, repo) = Build();
        repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Review>());
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await sut.List(page: 1, pageSize: 9999);

        repo.Verify(r => r.ListAsync(0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_calculates_skip_from_page()
    {
        var (sut, repo) = Build();
        repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Review>());
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await sut.List(page: 3, pageSize: 25);

        repo.Verify(r => r.ListAsync(50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_returns_404_when_review_missing()
    {
        var (sut, repo) = Build();
        repo.Setup(r => r.GetAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Review?)null);

        var result = await sut.Get(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_returns_review_when_found()
    {
        var (sut, repo) = Build();
        var review = new Review { Id = 1, PrTitle = "x", RepoOwner = "o", RepoName = "r" };
        repo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(review);

        var result = await sut.Get(1, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.Should().Be(review);
    }
}
