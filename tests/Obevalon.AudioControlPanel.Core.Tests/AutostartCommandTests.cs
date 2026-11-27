// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Autostart;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class AutostartCommandTests
{
    [Fact]
    public void Build_QuotesPathAndAddsTray()
    {
        Assert.Equal("\"C:\\Tools\\OACP\\OACP.exe\" --tray", AutostartCommand.Build(@"C:\Tools\OACP\OACP.exe"));
    }

    [Theory]
    [InlineData("\"C:\\My Tools\\OACP.exe\" --tray", "C:\\My Tools\\OACP.exe")]
    [InlineData("C:\\My Tools\\OACP.exe --tray", "C:\\My Tools\\OACP.exe")]
    [InlineData("C:\\Tools\\other --tray", "C:\\Tools\\other")]
    [InlineData("\"unterminated", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void GetExecutablePath_ParsesCommand(string? command, string? expected)
    {
        Assert.Equal(expected, AutostartCommand.GetExecutablePath(command));
    }

    [Fact]
    public void PointsTo_ComparesPathsCaseInsensitively()
    {
        var exe = Path.Combine(Path.GetTempPath(), "OACP", "OACP.exe");

        Assert.True(AutostartCommand.PointsTo(AutostartCommand.Build(exe), exe));
        Assert.True(AutostartCommand.PointsTo(AutostartCommand.Build(exe.ToUpperInvariant()), exe) || !OperatingSystem.IsWindows());
        Assert.False(AutostartCommand.PointsTo(AutostartCommand.Build(Path.Combine(Path.GetTempPath(), "Old", "OACP.exe")), exe));
        Assert.False(AutostartCommand.PointsTo(null, exe));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(new byte[0], false)]
    [InlineData(new byte[] { 0x02, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x06, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x03, 1, 2, 3 }, true)]
    [InlineData(new byte[] { 0x07, 1, 2, 3 }, true)]
    public void StartupApproved_OddFirstByteMeansDisabled(byte[]? data, bool disabled)
    {
        Assert.Equal(disabled, AutostartCommand.IsDisabledByStartupApproved(data));
    }
}
