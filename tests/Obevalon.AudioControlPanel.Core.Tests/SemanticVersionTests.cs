// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Updates;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3-beta.1+abc", "1.2.3-beta.1")]
    [InlineData("0.0.0-dev", "0.0.0-dev")]
    public void Parse_ReadsVersions(string text, string expected)
    {
        Assert.Equal(expected, SemanticVersion.Parse(text)!.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("01.2.3")]
    [InlineData("1.2.x")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-beta..1")]
    public void Parse_RejectsInvalid(string? text)
    {
        Assert.Null(SemanticVersion.Parse(text));
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0")]
    [InlineData("1.9.0", "1.10.0")]
    [InlineData("1.0.9", "1.0.10")]
    [InlineData("1.0.0-alpha", "1.0.0")]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    [InlineData("0.0.0-dev", "0.1.0")]
    public void Ordering_FollowsSemVer(string lower, string higher)
    {
        var a = SemanticVersion.Parse(lower)!;
        var b = SemanticVersion.Parse(higher)!;

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_IgnoresBuildMetadata()
    {
        Assert.Equal(SemanticVersion.Parse("1.2.3+a"), SemanticVersion.Parse("v1.2.3"));
        Assert.True(SemanticVersion.Parse("1.2.3") >= SemanticVersion.Parse("1.2.3"));
    }
}
