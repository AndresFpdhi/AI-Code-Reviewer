using System.Security.Cryptography;
using System.Text;
using CodeReviewer.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeReviewer.Api.Middleware;

public class GitHubSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GitHubAppOptions _options;
    private readonly ILogger<GitHubSignatureMiddleware> _logger;

    public GitHubSignatureMiddleware(
        RequestDelegate next,
        IOptions<GitHubAppOptions> options,
        ILogger<GitHubSignatureMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var body = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        var header = context.Request.Headers["X-Hub-Signature-256"].ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("sha256=", StringComparison.Ordinal))
        {
            _logger.LogWarning("Webhook missing or invalid X-Hub-Signature-256 header");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!IsSignatureValid(header[7..], body, _options.WebhookSecret))
        {
            _logger.LogWarning("Webhook signature verification failed");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static async Task<byte[]> ReadBodyAsync(Stream body)
    {
        using var ms = new MemoryStream();
        await body.CopyToAsync(ms);
        return ms.ToArray();
    }

    public static bool IsSignatureValid(string expectedHex, byte[] body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(body);
        var expected = Convert.FromHexString(expectedHex);
        return expected.Length == computed.Length
            && CryptographicOperations.FixedTimeEquals(expected, computed);
    }
}
