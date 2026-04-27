using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CodeReviewer.Api.BackgroundJobs;
using CodeReviewer.Core.Models;
using CodeReviewer.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CodeReviewer.Tests;

public class WebhookControllerTests : IClassFixture<WebhookControllerTests.Factory>
{
    private const string Secret = "webhook-secret";
    private readonly Factory _factory;

    public WebhookControllerTests(Factory factory) => _factory = factory;

    private static StringContent SignedJson(string body, out string signature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        signature = "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return new StringContent(body, Encoding.UTF8, "application/json");
    }

    private async Task<HttpResponseMessage> PostAsync(string body, string eventType)
    {
        var content = SignedJson(body, out var sig);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/github/webhook") { Content = content };
        req.Headers.Add("X-Hub-Signature-256", sig);
        req.Headers.Add("X-GitHub-Event", eventType);
        return await _factory.CreateClient().SendAsync(req);
    }

    [Fact]
    public async Task Rejects_request_with_bad_signature()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/github/webhook")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Hub-Signature-256", "sha256=deadbeef");
        req.Headers.Add("X-GitHub-Event", "pull_request");

        var resp = await _factory.CreateClient().SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ignores_non_pull_request_events()
    {
        var resp = await PostAsync("{\"action\":\"opened\"}", "issues");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resp.Content.ReadFromJsonAsync<IgnoredPayload>();
        payload!.Ignored.Should().BeTrue();
    }

    [Fact]
    public async Task Ignores_pull_request_actions_we_dont_handle()
    {
        var body = """
            {"action":"closed","pull_request":{"number":1,"head":{"sha":"a"}},
             "repository":{"name":"r","owner":{"login":"o"}},"installation":{"id":1}}
            """;
        var resp = await PostAsync(body, "pull_request");

        var payload = await resp.Content.ReadFromJsonAsync<IgnoredPayload>();
        payload!.Ignored.Should().BeTrue();
    }

    [Fact]
    public async Task Queues_review_on_pull_request_opened()
    {
        var body = """
            {"action":"opened","pull_request":{"number":42,"head":{"sha":"abc123"}},
             "repository":{"name":"my-repo","owner":{"login":"my-user"}},"installation":{"id":99}}
            """;

        var resp = await PostAsync(body, "pull_request");

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var queue = (CapturingQueue)_factory.Services.GetRequiredService<IReviewQueue>();
        queue.LastReceived.Should().NotBeNull();
        queue.LastReceived!.RepoOwner.Should().Be("my-user");
        queue.LastReceived.RepoName.Should().Be("my-repo");
        queue.LastReceived.PrNumber.Should().Be(42);
        queue.LastReceived.HeadSha.Should().Be("abc123");
        queue.LastReceived.InstallationId.Should().Be(99);
    }

    private record IgnoredPayload(bool Ignored, string Reason);

    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"webhook-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("GitHubApp:WebhookSecret", Secret);
            builder.UseSetting("GitHubApp:AppId", "1");
            builder.UseSetting("GitHubApp:PrivateKeyPem", "-");
            builder.UseSetting("Groq:ApiKey", "k");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");

            builder.ConfigureServices(services =>
            {
                services.RemoveAllOf<IReviewQueue>();
                services.AddSingleton<IReviewQueue, CapturingQueue>();
                services.RemoveAllOf<IHostedService>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
        }
    }

    private class CapturingQueue : IReviewQueue
    {
        public ReviewRequest? LastReceived { get; private set; }

        public ValueTask EnqueueAsync(ReviewRequest request, CancellationToken ct = default)
        {
            LastReceived = request;
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ReviewRequest> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAllOf<T>(this IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
            if (services[i].ServiceType == typeof(T)) services.RemoveAt(i);
    }
}
