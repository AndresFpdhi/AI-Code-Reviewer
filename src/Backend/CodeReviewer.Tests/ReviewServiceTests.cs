using CodeReviewer.Core.Entities;
using CodeReviewer.Core.Models;
using CodeReviewer.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CodeReviewer.Tests;

public class ReviewServiceTests
{
    [Fact]
    public async Task Review_pipeline_fetches_calls_ai_posts_and_persists()
    {
        var prCtx = new PullRequestContext("Title", "Body", "diff", "https://gh/x");
        var aiResult = new AiReviewResult
        {
            Summary = "looks ok",
            Score = 7,
            Comments =
            {
                new AiReviewComment { Path = "a.cs", Line = 10, Body = "consider null-check" }
            }
        };

        var github = new Mock<IGitHubAppClient>();
        github.Setup(x => x.GetPullRequestContextAsync(1, "owner", "repo", 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prCtx);

        var groq = new Mock<IGroqClient>();
        groq.Setup(x => x.ReviewAsync(prCtx, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var saved = new List<Review>();
        var repo = new Mock<IReviewRepository>();
        repo.Setup(x => x.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Callback<Review, CancellationToken>((r, _) => saved.Add(r))
            .Returns(Task.CompletedTask);

        var sut = new ReviewService(github.Object, groq.Object, repo.Object, NullLogger<ReviewService>.Instance);
        var request = new ReviewRequest(1, "owner", "repo", 42, "abc123");

        await sut.ReviewAsync(request);

        github.Verify(x => x.PostReviewAsync(1, "owner", "repo", 42, "abc123", aiResult, It.IsAny<CancellationToken>()),
            Times.Once);
        saved.Should().HaveCount(1);
        saved[0].PrTitle.Should().Be("Title");
        saved[0].Score.Should().Be(7);
        saved[0].CommentCount.Should().Be(1);
        saved[0].RawJson.Should().Contain("consider null-check");
    }

    [Fact]
    public async Task Failure_in_groq_does_not_persist_a_review()
    {
        var github = new Mock<IGitHubAppClient>();
        github.Setup(x => x.GetPullRequestContextAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PullRequestContext("t", "b", "diff", "u"));

        var groq = new Mock<IGroqClient>();
        groq.Setup(x => x.ReviewAsync(It.IsAny<PullRequestContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rate limited"));

        var repo = new Mock<IReviewRepository>(MockBehavior.Strict);

        var sut = new ReviewService(github.Object, groq.Object, repo.Object, NullLogger<ReviewService>.Instance);

        await FluentActions.Invoking(() => sut.ReviewAsync(new ReviewRequest(1, "o", "r", 1, "sha")))
            .Should().ThrowAsync<InvalidOperationException>();

        repo.Verify(x => x.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
