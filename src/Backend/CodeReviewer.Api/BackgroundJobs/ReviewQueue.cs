using System.Threading.Channels;
using CodeReviewer.Core.Models;

namespace CodeReviewer.Api.BackgroundJobs;

public interface IReviewQueue
{
    ValueTask EnqueueAsync(ReviewRequest request, CancellationToken ct = default);
    IAsyncEnumerable<ReviewRequest> ReadAllAsync(CancellationToken ct);
}

public class ReviewQueue : IReviewQueue
{
    private readonly Channel<ReviewRequest> _channel = Channel.CreateBounded<ReviewRequest>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(ReviewRequest request, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(request, ct);

    public IAsyncEnumerable<ReviewRequest> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
