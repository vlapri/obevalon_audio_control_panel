// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class StartupOptionsTests
{
    [Fact]
    public void NoArguments_GivesDefaults()
    {
        var options = StartupOptions.Parse([]);

        Assert.Equal(new StartupOptions(), options);
    }

    [Fact]
    public void AllArguments_AreRecognized()
    {
        var options = StartupOptions.Parse(["--tray", "--log", "--updated", "--wait-pid", "1234"]);

        Assert.True(options.StartInTray);
        Assert.True(options.EnableLogging);
        Assert.True(options.Updated);
        Assert.Equal(1234, options.WaitForProcessId);
    }

    [Fact]
    public void Arguments_AreCaseInsensitive()
    {
        var options = StartupOptions.Parse(["--TRAY", " --Log "]);

        Assert.True(options.StartInTray);
        Assert.True(options.EnableLogging);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("")]
    public void InvalidWaitPid_IsIgnored(string value)
    {
        var options = StartupOptions.Parse(["--wait-pid", value]);

        Assert.Null(options.WaitForProcessId);
    }

    [Fact]
    public void WaitPidWithoutValue_IsIgnored()
    {
        var options = StartupOptions.Parse(["--wait-pid"]);

        Assert.Null(options.WaitForProcessId);
    }

    [Fact]
    public void InvalidWaitPid_DoesNotSwallowNextArgument()
    {
        var options = StartupOptions.Parse(["--wait-pid", "--tray"]);

        Assert.Null(options.WaitForProcessId);
        Assert.True(options.StartInTray);
    }

    [Fact]
    public void UnknownArguments_AreIgnored()
    {
        var options = StartupOptions.Parse(["--unknown", "foo", "--tray"]);

        Assert.True(options.StartInTray);
        Assert.False(options.EnableLogging);
    }
}
