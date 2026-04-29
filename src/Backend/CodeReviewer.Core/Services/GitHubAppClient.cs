using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeReviewer.Core.Models;
using CodeReviewer.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace CodeReviewer.Core.Services;

public class GitHubAppClient : IGitHubAppClient
{
    private readonly HttpClient _http;
    private readonly GitHubAppOptions _options;
    private readonly ILogger<GitHubAppClient> _logger;

    public GitHubAppClient(HttpClient http, IOptions<GitHubAppOptions> options, ILogger<GitHubAppClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PullRequestContext> GetPullRequestContextAsync(
        long installationId, string owner, string repo, int prNumber, CancellationToken ct = default)
    {
        var client = await CreateInstallationClientAsync(installationId);
        var pr = await client.PullRequest.Get(owner, repo, prNumber);
        var diff = await FetchDiffAsync(installationId, owner, repo, prNumber, ct);

        return new PullRequestContext(
            Title: pr.Title ?? string.Empty,
            Body: pr.Body ?? string.Empty,
            UnifiedDiff: diff,
            Url: pr.HtmlUrl ?? string.Empty);
    }

    public async Task PostReviewAsync(
        long installationId, string owner, string repo, int prNumber, string commitSha,
        AiReviewResult review, CancellationToken ct = default)
    {
        var client = await CreateInstallationClientAsync(installationId);
        var body = $"**AI review (score {review.Score}/10)**\n\n{review.Summary}";

        var inline = review.Comments
            .Where(c => !string.IsNullOrWhiteSpace(c.Path) && c.Line > 0)
            .Take(20)
            .Select(c => new DraftPullRequestReviewComment(c.Body, c.Path, c.Line))
            .ToList();

        var request = new PullRequestReviewCreate
        {
            Body = body,
            Event = PullRequestReviewEvent.Comment,
            CommitId = commitSha
        };
        foreach (var c in inline) request.Comments.Add(c);

        try
        {
            await client.PullRequest.Review.Create(owner, repo, prNumber, request);
        }
        catch (ApiValidationException ex)
        {
            _logger.LogWarning(ex, "Inline comments rejected, posting summary-only review");
            var fallback = new PullRequestReviewCreate
            {
                Body = body,
                Event = PullRequestReviewEvent.Comment,
                CommitId = commitSha
            };
            await client.PullRequest.Review.Create(owner, repo, prNumber, fallback);
        }
    }

    private async Task<GitHubClient> CreateInstallationClientAsync(long installationId)
    {
        var jwt = GenerateJwt();
        var appClient = new GitHubClient(new Octokit.ProductHeaderValue(_options.UserAgent))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };
        var token = await appClient.GitHubApps.CreateInstallationToken(installationId);
        return new GitHubClient(new Octokit.ProductHeaderValue(_options.UserAgent))
        {
            Credentials = new Credentials(token.Token)
        };
    }

    private string GenerateJwt()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.ResolvePrivateKey());

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var headerJson = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(new
        {
            iat = now - 60,
            exp = now + 540,
            iss = _options.AppId
        });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<string> FetchDiffAsync(
        long installationId, string owner, string repo, int prNumber, CancellationToken ct)
    {
        var jwt = GenerateJwt();
        using var jwtClient = new HttpClient();
        jwtClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
        jwtClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        jwtClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var tokenResp = await jwtClient.PostAsync(
            $"https://api.github.com/app/installations/{installationId}/access_tokens", null, ct);
        tokenResp.EnsureSuccessStatusCode();
        var tokenJson = await tokenResp.Content.ReadAsStringAsync(ct);
        var token = System.Text.Json.JsonDocument.Parse(tokenJson).RootElement.GetProperty("token").GetString();

        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));

        var resp = await _http.GetAsync(
            $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
