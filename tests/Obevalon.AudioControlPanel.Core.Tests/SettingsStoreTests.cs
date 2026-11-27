// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "oacp-tests-" + Guid.NewGuid().ToString("N"));

    public SettingsStoreTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Load_MissingFile_GivesDefaults()
    {
        var store = new IniSettingsStore(Path.Combine(_directory, "settings.ini"), isPortable: true);

        var settings = store.Load();

        Assert.Equal("auto", settings.Language);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips_AndLeavesNoTemporaryFile()
    {
        var path = Path.Combine(_directory, "sub", "settings.ini");
        var store = new IniSettingsStore(path, isPortable: false);
        var settings = new AppSettings { Language = "cs" };
        settings.Outputs.Add(new OutputDeviceSettings("id") { Note = "Poznámka s diakritikou" });

        store.Save(settings);
        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal("cs", loaded.Language);
        Assert.Equal("Poznámka s diakritikou", loaded.Outputs.Single().Note);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_CorruptedFile_DoesNotThrow()
    {
        var path = Path.Combine(_directory, "settings.ini");
        File.WriteAllBytes(path, [0xFF, 0xFE, 0x00, 0x5B, 0x00, 0x00, 0x13]);
        var store = new IniSettingsStore(path, isPortable: true);

        var settings = store.Load();

        Assert.NotNull(settings);
    }

    [Fact]
    public void Resolver_PrefersExecutableDirectory_WhenWritable()
    {
        var location = SettingsPathResolver.Resolve(_directory, "/appdata", _ => true);

        Assert.True(location.IsPortable);
        Assert.Equal(Path.Combine(_directory, "settings.ini"), location.Path);
    }

    [Fact]
    public void Resolver_FallsBackToAppData_WhenNotWritable()
    {
        var location = SettingsPathResolver.Resolve(_directory, "/appdata", _ => false);

        Assert.False(location.IsPortable);
        Assert.Equal(Path.Combine("/appdata", "Obevalon", "AudioControlPanel", "settings.ini"), location.Path);
    }

    [Fact]
    public void IsDirectoryWritable_DetectsWritableAndMissingDirectories()
    {
        Assert.True(SettingsPathResolver.IsDirectoryWritable(_directory));
        Assert.False(SettingsPathResolver.IsDirectoryWritable(Path.Combine(_directory, "does-not-exist")));
        Assert.Empty(Directory.GetFiles(_directory));
    }
}
