using CodeReviewer.Api.BackgroundJobs;
using CodeReviewer.Core.Models;
using FluentAssertions;
using Xunit;

namespace CodeReviewer.Tests;

public class ReviewQueueTests
{
    [Fact]
    public async Task Enqueued_items_are_read_in_fifo_order()
    {
        var queue = new ReviewQueue();
        await queue.EnqueueAsync(new ReviewRequest(1, "o", "r", 1, "sha1"));
        await queue.EnqueueAsync(new ReviewRequest(1, "o", "r", 2, "sha2"));

        var read = new List<int>();
        using var cts = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                read.Add(item.PrNumber);
                if (read.Count == 2) cts.Cancel();
            }
        });

        try { await task; } catch (OperationCanceledException) { }

        read.Should().Equal(1, 2);
    }

    [Fact]
    public async Task Reader_blocks_until_item_available()
    {
        var queue = new ReviewQueue();
        using var cts = new CancellationTokenSource();
        var received = new TaskCompletionSource<ReviewRequest>();

        var reader = Task.Run(async () =>
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                received.SetResult(item);
                cts.Cancel();
                break;
            }
        });

        received.Task.IsCompleted.Should().BeFalse();
        await queue.EnqueueAsync(new ReviewRequest(1, "o", "r", 42, "sha"));

        var item = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));
        item.PrNumber.Should().Be(42);
        try { await reader; } catch (OperationCanceledException) { }
    }
}
