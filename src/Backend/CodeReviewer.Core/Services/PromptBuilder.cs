using CodeReviewer.Core.Models;
using CodeReviewer.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeReviewer.Core.Services;

public class PromptBuilder : IPromptBuilder
{
    private readonly GroqOptions _options;

    public PromptBuilder(IOptions<GroqOptions> options)
    {
        _options = options.Value;
    }

    public (string SystemPrompt, string UserPrompt) Build(PullRequestContext context)
    {
        const string system = """
            You are a senior software engineer performing a pull request review.
            Focus on correctness bugs, security issues, race conditions, error handling,
            and maintainability problems. Ignore stylistic nits that linters/formatters
            already handle. Be concrete: cite the file path and line number for each comment.

            Respond ONLY with a single JSON object in this exact shape:
            {
              "summary": "A 2-4 sentence overall summary of the change and its quality.",
              "score": 1-10 integer (10 = ship it, 1 = reject),
              "comments": [
                { "path": "relative/file/path", "line": <int>, "body": "specific actionable feedback" }
              ]
            }
            Return at most 10 comments. If the change is trivial, return an empty comments array.
            Do not wrap the JSON in code fences. Do not include any text outside the JSON.
            """;

        var diff = TruncateDiff(context.UnifiedDiff, _options.MaxDiffChars);

        var user = $"""
            PR Title: {context.Title}
            PR URL: {context.Url}

            PR Description:
            {(string.IsNullOrWhiteSpace(context.Body) ? "(no description)" : context.Body)}

            Unified diff:
            ```diff
            {diff}
            ```
            """;

        return (system, user);
    }

    private static string TruncateDiff(string diff, int maxChars)
    {
        if (string.IsNullOrEmpty(diff) || diff.Length <= maxChars) return diff;
        return diff[..maxChars] + $"\n\n[... diff truncated, {diff.Length - maxChars} chars omitted ...]";
    }
}
