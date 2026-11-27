// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Diagnostics;

namespace Obevalon.AudioControlPanel.Core.Tests;

public sealed class AppLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "oacp-log-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Enable_WritesEntriesAndRemovesOldFiles()
    {
        Directory.CreateDirectory(_directory);
        var old = Path.Combine(_directory, "oacp-2000-01-01.log");
        File.WriteAllText(old, "old");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-30));

        AppLog.Enable(_directory);
        AppLog.Info("hello");
        AppLog.Error("failed", new InvalidOperationException("boom"));

        Assert.False(File.Exists(old));
        var text = File.ReadAllText(Path.Combine(_directory, AppLog.FileNameFor(DateTime.Now)));
        Assert.Contains("INFO  hello", text, StringComparison.Ordinal);
        Assert.Contains("ERROR failed | InvalidOperationException: boom", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FileName_UsesIsoDate()
    {
        Assert.Equal("oacp-2027-03-09.log", AppLog.FileNameFor(new DateTime(2027, 3, 9)));
    }
}
