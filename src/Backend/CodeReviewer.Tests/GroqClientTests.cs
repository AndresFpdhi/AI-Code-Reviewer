using System.Net;
using System.Text;
using CodeReviewer.Core.Models;
using CodeReviewer.Core.Options;
using CodeReviewer.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeReviewer.Tests;

public class GroqClientTests
{
    private static GroqClient Build(StubHandler handler, GroqOptions? opts = null)
    {
        opts ??= new GroqOptions { ApiKey = "test-key", MaxRetries = 3 };
        var http = new HttpClient(handler);
        var prompt = new PromptBuilder(Options.Create(opts));
        return new GroqClient(http, prompt, Options.Create(opts), NullLogger<GroqClient>.Instance);
    }

    private static PullRequestContext Ctx() => new("title", "body", "diff", "https://gh/x");

    private const string SuccessJson = """
        {"choices":[{"message":{"role":"assistant","content":"{\"summary\":\"ok\",\"score\":8,\"comments\":[{\"path\":\"a.cs\",\"line\":5,\"body\":\"nit\"}]}"}}]}
        """;

    [Fact]
    public async Task Successful_call_parses_inner_json_into_result()
    {
        var handler = new StubHandler(_ => StubHandler.JsonOk(SuccessJson));
        var sut = Build(handler);

        var result = await sut.ReviewAsync(Ctx());

        result.Summary.Should().Be("ok");
        result.Score.Should().Be(8);
        result.Comments.Should().ContainSingle().Which.Body.Should().Be("nit");
    }

    [Fact]
    public async Task Authorization_header_is_sent_with_bearer_token()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(req =>
        {
            captured = req;
            return StubHandler.JsonOk(SuccessJson);
        });
        var sut = Build(handler);

        await sut.ReviewAsync(Ctx());

        captured!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured.Headers.Authorization.Parameter.Should().Be("test-key");
    }

    [Fact]
    public async Task Retries_on_429_then_succeeds()
    {
        var calls = 0;
        var handler = new StubHandler(_ =>
        {
            calls++;
            return calls < 2
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : StubHandler.JsonOk(SuccessJson);
        });
        var sut = Build(handler, new GroqOptions { ApiKey = "k", MaxRetries = 3 });

        var result = await sut.ReviewAsync(Ctx());

        calls.Should().Be(2);
        result.Score.Should().Be(8);
    }

    [Fact]
    public async Task Throws_when_all_retries_exhausted()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = Build(handler, new GroqOptions { ApiKey = "k", MaxRetries = 2 });

        await FluentActions.Invoking(() => sut.ReviewAsync(Ctx()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));

        public static HttpResponseMessage JsonOk(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
