// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Logic;
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class DeviceCatalogTests
{
    private static AudioDevice Device(string id, string name, AudioDeviceState state = AudioDeviceState.Active) =>
        new(id, name, AudioFlow.Output, state);

    [Fact]
    public void NewDevices_AreAddedAvailableFirstThenByName()
    {
        var settings = new AppSettings();
        var devices = new[]
        {
            Device("z", "Zeta"),
            Device("u", "Alpha unplugged", AudioDeviceState.Unplugged),
            Device("a", "Alpha"),
        };

        var entries = DeviceCatalog.Merge(settings, devices, "z", out var changed);

        Assert.True(changed);
        Assert.Equal(["a", "z", "u"], entries.Select(e => e.Id));
        Assert.Equal(["a", "z", "u"], settings.Outputs.Select(o => o.Id));
        Assert.True(entries.Single(e => e.Id == "z").IsDefault);
        Assert.All(settings.Outputs, o => Assert.False(o.InCycle));
    }

    [Fact]
    public void KnownDevices_KeepUserOrderAndSettings()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("hdmi") { SystemName = "HDMI", Name = "Projektor", Note = "obývák", InCycle = true });
        settings.Outputs.Add(new OutputDeviceSettings("jack") { SystemName = "Speakers", Hidden = true });

        var entries = DeviceCatalog.Merge(settings, [Device("jack", "Speakers"), Device("hdmi", "HDMI")], null, out var changed);

        Assert.False(changed);
        Assert.Equal(["hdmi", "jack"], entries.Select(e => e.Id));
        Assert.Equal("Projektor", entries[0].DisplayName);
        Assert.Equal("obývák", entries[0].Note);
        Assert.True(entries[0].InCycle);
        Assert.True(entries[1].Hidden);
        Assert.Equal("Speakers", entries[1].DisplayName);
    }

    [Fact]
    public void MissingDevice_IsKeptAsMissing_WithLastKnownName()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("old") { SystemName = "Old HDMI" });

        var entries = DeviceCatalog.Merge(settings, [], null, out var changed);

        Assert.False(changed);
        var entry = Assert.Single(entries);
        Assert.True(entry.IsMissing);
        Assert.False(entry.IsAvailable);
        Assert.Equal("Old HDMI", entry.DisplayName);
    }

    [Fact]
    public void RenamedDevice_UpdatesSystemName()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("jack") { SystemName = "Speakers" });

        var entries = DeviceCatalog.Merge(settings, [Device("jack", "Repro")], null, out var changed);

        Assert.True(changed);
        Assert.Equal("Repro", settings.Outputs[0].SystemName);
        Assert.Equal("Repro", entries[0].DisplayName);
    }

    [Fact]
    public void DeviceIds_AreCaseInsensitive()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("{ABC}") { SystemName = "X" });

        var entries = DeviceCatalog.Merge(settings, [Device("{abc}", "X")], "{abc}", out var changed);

        Assert.False(changed);
        Assert.Single(entries);
        Assert.True(entries[0].IsDefault);
        Assert.True(entries[0].IsAvailable);
    }

    [Theory]
    [InlineData("b", -1, new[] { "b", "a", "c" }, true)]
    [InlineData("b", 1, new[] { "a", "c", "b" }, true)]
    [InlineData("a", -1, new[] { "a", "b", "c" }, false)]
    [InlineData("c", 1, new[] { "a", "b", "c" }, false)]
    [InlineData("x", 1, new[] { "a", "b", "c" }, false)]
    public void Move_ChangesOrder(string id, int offset, string[] expected, bool moved)
    {
        var settings = new AppSettings();
        foreach (var output in new[] { "a", "b", "c" })
        {
            settings.Outputs.Add(new OutputDeviceSettings(output));
        }

        Assert.Equal(moved, DeviceCatalog.Move(settings, id, offset));
        Assert.Equal(expected, settings.Outputs.Select(o => o.Id));
    }

    [Fact]
    public void Remove_DeletesDevice()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("a"));

        Assert.True(DeviceCatalog.Remove(settings, "A"));
        Assert.False(DeviceCatalog.Remove(settings, "a"));
        Assert.Empty(settings.Outputs);
    }
}
