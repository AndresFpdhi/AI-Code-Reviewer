using System.Text.Json;
using CodeReviewer.Api.BackgroundJobs;
using CodeReviewer.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewer.Api.Controllers;

[ApiController]
[Route("api/github/webhook")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IReviewQueue _queue;
    private readonly ILogger<GitHubWebhookController> _logger;

    public GitHubWebhookController(IReviewQueue queue, ILogger<GitHubWebhookController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var eventType = Request.Headers["X-GitHub-Event"].ToString();
        if (!string.Equals(eventType, "pull_request", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { ignored = true, reason = $"event {eventType} not handled" });
        }

        Request.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        var root = doc.RootElement;

        var action = root.GetProperty("action").GetString();
        if (action is not ("opened" or "synchronize" or "reopened"))
        {
            return Ok(new { ignored = true, reason = $"action {action} not handled" });
        }

        var pr = root.GetProperty("pull_request");
        var repo = root.GetProperty("repository");
        var installation = root.GetProperty("installation");

        var request = new ReviewRequest(
            InstallationId: installation.GetProperty("id").GetInt64(),
            RepoOwner: repo.GetProperty("owner").GetProperty("login").GetString() ?? string.Empty,
            RepoName: repo.GetProperty("name").GetString() ?? string.Empty,
            PrNumber: pr.GetProperty("number").GetInt32(),
            HeadSha: pr.GetProperty("head").GetProperty("sha").GetString() ?? string.Empty);

        await _queue.EnqueueAsync(request, ct);
        _logger.LogInformation("Queued review for {Owner}/{Repo}#{Pr}",
            request.RepoOwner, request.RepoName, request.PrNumber);

        return Accepted(new { queued = true });
    }
}
