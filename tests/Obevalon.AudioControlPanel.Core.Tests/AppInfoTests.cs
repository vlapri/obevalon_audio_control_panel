// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+abcdef0", "1.2.3")]
    [InlineData("1.0.0-beta.1+sha", "1.0.0-beta.1")]
    [InlineData(null, "0.0.0-dev")]
    [InlineData("", "0.0.0-dev")]
    [InlineData("  ", "0.0.0-dev")]
    public void NormalizeVersion_RemovesBuildMetadata(string? input, string expected)
    {
        Assert.Equal(expected, AppInfo.NormalizeVersion(input));
    }

    [Fact]
    public void Version_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
    }
}
