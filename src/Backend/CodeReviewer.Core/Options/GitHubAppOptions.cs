namespace CodeReviewer.Core.Options;

public class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    public string AppId { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "AI-Code-Reviewer";
}
