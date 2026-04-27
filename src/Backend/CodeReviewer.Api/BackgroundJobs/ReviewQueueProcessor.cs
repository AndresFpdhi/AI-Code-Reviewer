using CodeReviewer.Core.Services;

namespace CodeReviewer.Api.BackgroundJobs;

public class ReviewQueueProcessor : BackgroundService
{
    private readonly IReviewQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReviewQueueProcessor> _logger;

    public ReviewQueueProcessor(
        IReviewQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReviewQueueProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Review queue processor started");
        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReviewService>();
                await service.ReviewAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process review for {Owner}/{Repo}#{Pr}",
                    request.RepoOwner, request.RepoName, request.PrNumber);
            }
        }
    }
}
