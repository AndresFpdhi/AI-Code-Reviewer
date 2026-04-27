namespace CodeReviewer.Core.Models;

public record ReviewRequest(
    long InstallationId,
    string RepoOwner,
    string RepoName,
    int PrNumber,
    string HeadSha);
