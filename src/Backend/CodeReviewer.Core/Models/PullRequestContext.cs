namespace CodeReviewer.Core.Models;

public record PullRequestContext(
    string Title,
    string Body,
    string UnifiedDiff,
    string Url);
