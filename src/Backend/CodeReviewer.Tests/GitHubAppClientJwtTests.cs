using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeReviewer.Core.Options;
using CodeReviewer.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeReviewer.Tests;

/// <summary>
/// Exercises the JWT generation path that previously crashed with
/// "END RSA PRIVATE KEY was not found" when using GitHubJwt + BouncyCastle.NetCore.
/// The fix replaced both with .NET's built-in RSA.ImportFromPem + manual JWT signing.
/// </summary>
public class GitHubAppClientJwtTests
{
    private static (string PrivateKeyPem, RSA PublicKey) GenerateKeyPair()
    {
        var rsa = RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();
        var pub = RSA.Create();
        pub.ImportRSAPublicKey(rsa.ExportRSAPublicKey(), out _);
        return (pem, pub);
    }

    private static GitHubAppClient BuildClient(string pem, string appId = "12345")
    {
        var options = Options.Create(new GitHubAppOptions
        {
            AppId = appId,
            PrivateKeyPem = pem,
            UserAgent = "test"
        });
        return new GitHubAppClient(new HttpClient(), options, NullLogger<GitHubAppClient>.Instance);
    }

    private static string InvokeGenerateJwt(GitHubAppClient client) =>
        (string)typeof(GitHubAppClient)
            .GetMethod("GenerateJwt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(client, null)!;

    [Fact]
    public void GenerateJwt_does_not_throw_for_pkcs1_pem()
    {
        var (pem, _) = GenerateKeyPair();
        var client = BuildClient(pem);

        var act = () => InvokeGenerateJwt(client);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateJwt_does_not_throw_for_pkcs8_pem()
    {
        var rsa = RSA.Create(2048);
        var pkcs8Pem = rsa.ExportPkcs8PrivateKeyPem();
        var client = BuildClient(pkcs8Pem);

        var act = () => InvokeGenerateJwt(client);

        act.Should().NotThrow("ImportFromPem handles both PKCS#1 and PKCS#8 formats");
    }

    [Fact]
    public void GenerateJwt_does_not_throw_for_pem_with_crlf_line_endings()
    {
        var (pem, _) = GenerateKeyPair();
        var crlfPem = pem.Replace("\n", "\r\n");
        var client = BuildClient(crlfPem);

        var act = () => InvokeGenerateJwt(client);

        act.Should().NotThrow("Windows-style line endings should be handled");
    }

    [Fact]
    public void GenerateJwt_returns_a_three_part_jwt()
    {
        var (pem, _) = GenerateKeyPair();
        var client = BuildClient(pem);

        var jwt = InvokeGenerateJwt(client);

        jwt.Split('.').Should().HaveCount(3, "a JWT is header.payload.signature");
    }

    [Fact]
    public void GenerateJwt_payload_contains_iss_iat_exp()
    {
        var (pem, _) = GenerateKeyPair();
        var client = BuildClient(pem, appId: "98765");

        var jwt = InvokeGenerateJwt(client);
        var parts = jwt.Split('.');
        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);

        doc.RootElement.GetProperty("iss").GetString().Should().Be("98765");
        doc.RootElement.GetProperty("iat").GetInt64().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("exp").GetInt64().Should().BeGreaterThan(
            doc.RootElement.GetProperty("iat").GetInt64());
    }

    [Fact]
    public void GenerateJwt_signature_verifies_with_public_key()
    {
        var (pem, publicKey) = GenerateKeyPair();
        var client = BuildClient(pem);

        var jwt = InvokeGenerateJwt(client);
        var parts = jwt.Split('.');
        var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        var signature = Base64UrlDecode(parts[2]);

        var ok = publicKey.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        ok.Should().BeTrue("JWT signature must verify against the corresponding public key");
    }

    [Fact]
    public void GenerateJwt_header_uses_RS256()
    {
        var (pem, _) = GenerateKeyPair();
        var client = BuildClient(pem);

        var jwt = InvokeGenerateJwt(client);
        var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(jwt.Split('.')[0]));
        using var doc = JsonDocument.Parse(headerJson);

        doc.RootElement.GetProperty("alg").GetString().Should().Be("RS256");
        doc.RootElement.GetProperty("typ").GetString().Should().Be("JWT");
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var b64 = input.Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2: b64 += "=="; break;
            case 3: b64 += "="; break;
        }
        return Convert.FromBase64String(b64);
    }
}
