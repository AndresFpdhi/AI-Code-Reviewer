namespace CodeReviewer.Core.Options;

public class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    public string AppId { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "AI-Code-Reviewer";

    public string ResolvePrivateKey()
    {
        if (!string.IsNullOrWhiteSpace(PrivateKeyPath) && File.Exists(PrivateKeyPath))
            return File.ReadAllText(PrivateKeyPath);
        return PrivateKeyPem;
    }
}
