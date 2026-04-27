using System.Security.Cryptography;
using System.Text;
using CodeReviewer.Api.Middleware;
using FluentAssertions;
using Xunit;

namespace CodeReviewer.Tests;

public class GitHubSignatureMiddlewareTests
{
    private const string Secret = "test-secret";

    private static string Sign(byte[] body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();
    }

    [Fact]
    public void Valid_signature_is_accepted()
    {
        var body = "{\"action\":\"opened\"}"u8.ToArray();
        var sig = Sign(body);
        GitHubSignatureMiddleware.IsSignatureValid(sig, body, Secret).Should().BeTrue();
    }

    [Fact]
    public void Tampered_body_is_rejected()
    {
        var body = "{\"action\":\"opened\"}"u8.ToArray();
        var sig = Sign(body);
        var tampered = "{\"action\":\"closed\"}"u8.ToArray();
        GitHubSignatureMiddleware.IsSignatureValid(sig, tampered, Secret).Should().BeFalse();
    }

    [Fact]
    public void Wrong_secret_is_rejected()
    {
        var body = "{\"x\":1}"u8.ToArray();
        var sig = Sign(body);
        GitHubSignatureMiddleware.IsSignatureValid(sig, body, "different-secret").Should().BeFalse();
    }

    [Fact]
    public void Malformed_hex_signature_returns_false()
    {
        var body = "{}"u8.ToArray();
        Action act = () => GitHubSignatureMiddleware.IsSignatureValid("not-hex", body, Secret);
        act.Should().Throw<FormatException>();
    }
}
