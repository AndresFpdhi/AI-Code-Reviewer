using CodeReviewer.Core.Models;
using CodeReviewer.Core.Options;
using CodeReviewer.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeReviewer.Tests;

public class PromptBuilderTests
{
    private static PromptBuilder Build(int maxChars = 60_000) =>
        new(Options.Create(new GroqOptions { MaxDiffChars = maxChars }));

    [Fact]
    public void System_prompt_demands_json_only_output()
    {
        var (system, _) = Build().Build(new PullRequestContext("t", "b", "diff", "url"));
        system.Should().Contain("JSON");
        system.Should().Contain("Do not wrap the JSON in code fences");
    }

    [Fact]
    public void User_prompt_includes_pr_metadata_and_diff()
    {
        var ctx = new PullRequestContext("My PR", "fixes bug", "@@ diff @@", "https://gh/x");
        var (_, user) = Build().Build(ctx);
        user.Should().Contain("My PR");
        user.Should().Contain("fixes bug");
        user.Should().Contain("@@ diff @@");
        user.Should().Contain("https://gh/x");
    }

    [Fact]
    public void Diff_truncation_kicks_in_above_max_chars()
    {
        var hugeDiff = new string('x', 200);
        var (_, user) = Build(maxChars: 50).Build(new PullRequestContext("t", "b", hugeDiff, "u"));
        user.Should().Contain("diff truncated");
        user.Should().Contain("150 chars omitted");
    }

    [Fact]
    public void Empty_pr_body_is_handled_gracefully()
    {
        var (_, user) = Build().Build(new PullRequestContext("t", "", "diff", "u"));
        user.Should().Contain("(no description)");
    }
}
