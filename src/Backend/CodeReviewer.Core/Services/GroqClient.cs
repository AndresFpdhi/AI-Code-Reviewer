using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeReviewer.Core.Models;
using CodeReviewer.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeReviewer.Core.Services;

public class GroqClient : IGroqClient
{
    private readonly HttpClient _http;
    private readonly IPromptBuilder _promptBuilder;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqClient> _logger;

    public GroqClient(
        HttpClient http,
        IPromptBuilder promptBuilder,
        IOptions<GroqOptions> options,
        ILogger<GroqClient> logger)
    {
        _http = http;
        _promptBuilder = promptBuilder;
        _options = options.Value;
        _logger = logger;

        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(_options.ApiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<AiReviewResult> ReviewAsync(PullRequestContext context, CancellationToken ct = default)
    {
        var (system, user) = _promptBuilder.Build(context);

        var body = new ChatRequest
        {
            Model = _options.Model,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Temperature = 0.2,
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = system },
                new() { Role = "user", Content = user }
            }
        };

        ChatResponse? response = null;
        Exception? lastException = null;

        for (var attempt = 0; attempt < _options.MaxRetries; attempt++)
        {
            try
            {
                using var resp = await _http.PostAsJsonAsync("chat/completions", body, ct);

                if (resp.StatusCode == HttpStatusCode.TooManyRequests || (int)resp.StatusCode >= 500)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning("Groq returned {Status}, retrying in {Delay}s (attempt {Attempt})",
                        resp.StatusCode, delay.TotalSeconds, attempt + 1);
                    await Task.Delay(delay, ct);
                    continue;
                }

                resp.EnsureSuccessStatusCode();
                response = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Groq call failed on attempt {Attempt}", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        if (response is null)
            throw new InvalidOperationException("Groq call failed after retries", lastException);

        var content = response.Choices.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("Groq returned no message content");

        var result = JsonSerializer.Deserialize<AiReviewResult>(content)
            ?? throw new InvalidOperationException("Could not parse AI review JSON");

        return result;
    }

    private class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = new();
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("response_format")] public ResponseFormat? ResponseFormat { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    private class ResponseFormat
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "json_object";
    }

    private class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = new();
    }

    private class Choice
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
    }
}
