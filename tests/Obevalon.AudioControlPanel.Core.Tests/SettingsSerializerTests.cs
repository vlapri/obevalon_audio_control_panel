// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class SettingsSerializerTests
{
    [Fact]
    public void EmptyDocument_GivesDefaults()
    {
        var settings = SettingsSerializer.Read(new IniDocument());

        Assert.Equal("auto", settings.Language);
        Assert.Equal(ThemeMode.Auto, settings.Theme);
        Assert.True(settings.CloseToTray);
        Assert.True(settings.SwitchCommunicationsDevice);
        Assert.True(settings.ShowSwitchOsd);
        Assert.False(settings.ShowHiddenDevices);
        Assert.Null(settings.MicrophoneDeviceId);
        Assert.Null(settings.Hotkeys.NextOutput);
        Assert.True(settings.Updates.AutoCheck);
        Assert.Empty(settings.Outputs);
    }

    [Fact]
    public void AllValues_RoundTrip()
    {
        var original = new AppSettings
        {
            Language = "cs",
            Theme = ThemeMode.Dark,
            CloseToTray = false,
            SwitchCommunicationsDevice = false,
            ShowSwitchOsd = false,
            ShowHiddenDevices = true,
            MicrophoneDeviceId = "{0.0.1.00000000}.{mic}",
            WindowLeft = 120.5,
            WindowTop = -30,
        };
        original.Hotkeys.NextOutput = "Ctrl+Alt+A";
        original.Hotkeys.ToggleMicMute = "Ctrl+Alt+M";
        original.Hotkeys.ShowWindow = "Win+Shift+O";
        original.Updates.AutoCheck = false;
        original.Updates.LastCheckUtc = new DateTimeOffset(2026, 11, 27, 10, 30, 0, TimeSpan.Zero);
        original.Updates.SkippedVersion = "1.2.0";
        original.Updates.ManifestUrl = "https://example.test/update.json";
        original.Outputs.Add(new OutputDeviceSettings("{0.0.0.00000000}.{hdmi}")
        {
            SystemName = "HDMI (NVIDIA)",
            Name = "Projektor",
            Note = "Velké repro; obývák",
            InCycle = true,
        });
        original.Outputs.Add(new OutputDeviceSettings("{0.0.0.00000000}.{jack}") { SystemName = "Speakers", Hidden = true });

        var text = SettingsSerializer.Write(original).ToString();
        var read = SettingsSerializer.Read(IniDocument.Parse(text));

        Assert.Equal("cs", read.Language);
        Assert.Equal(ThemeMode.Dark, read.Theme);
        Assert.False(read.CloseToTray);
        Assert.False(read.SwitchCommunicationsDevice);
        Assert.False(read.ShowSwitchOsd);
        Assert.True(read.ShowHiddenDevices);
        Assert.Equal("{0.0.1.00000000}.{mic}", read.MicrophoneDeviceId);
        Assert.Equal(120.5, read.WindowLeft);
        Assert.Equal(-30, read.WindowTop);
        Assert.Equal("Ctrl+Alt+A", read.Hotkeys.NextOutput);
        Assert.Equal("Ctrl+Alt+M", read.Hotkeys.ToggleMicMute);
        Assert.Equal("Win+Shift+O", read.Hotkeys.ShowWindow);
        Assert.False(read.Updates.AutoCheck);
        Assert.Equal(original.Updates.LastCheckUtc, read.Updates.LastCheckUtc);
        Assert.Equal("1.2.0", read.Updates.SkippedVersion);
        Assert.Equal("https://example.test/update.json", read.Updates.ManifestUrl);

        Assert.Equal(2, read.Outputs.Count);
        var hdmi = read.Outputs[0];
        Assert.Equal("{0.0.0.00000000}.{hdmi}", hdmi.Id);
        Assert.Equal("HDMI (NVIDIA)", hdmi.SystemName);
        Assert.Equal("Projektor", hdmi.Name);
        Assert.Equal("Velké repro; obývák", hdmi.Note);
        Assert.True(hdmi.InCycle);
        Assert.False(hdmi.Hidden);
        Assert.True(read.Outputs[1].Hidden);
    }

    [Fact]
    public void InvalidValues_FallBackToDefaults()
    {
        var settings = SettingsSerializer.Read(IniDocument.Parse("""
            [General]
            Language=klingon
            Theme=purple
            CloseToTray=maybe
            ShowSwitchOsd=
            [Window]
            Left=abc
            Top=NaN
            [Updates]
            LastCheckUtc=yesterday
            """));

        Assert.Equal("auto", settings.Language);
        Assert.Equal(ThemeMode.Auto, settings.Theme);
        Assert.True(settings.CloseToTray);
        Assert.True(settings.ShowSwitchOsd);
        Assert.Null(settings.WindowLeft);
        Assert.Null(settings.WindowTop);
        Assert.Null(settings.Updates.LastCheckUtc);
    }

    [Theory]
    [InlineData("1", ThemeMode.Auto)]
    [InlineData("LIGHT", ThemeMode.Light)]
    [InlineData("dark ; auto | light | dark", ThemeMode.Dark)]
    public void Theme_IsParsedByNameOnly(string value, ThemeMode expected)
    {
        var settings = SettingsSerializer.Read(IniDocument.Parse($"[General]\nTheme={value}"));

        Assert.Equal(expected, settings.Theme);
    }

    [Theory]
    [InlineData("yes", true)]
    [InlineData("0", false)]
    [InlineData("FALSE ; comment", false)]
    public void Booleans_AcceptCommonForms(string value, bool expected)
    {
        var settings = SettingsSerializer.Read(IniDocument.Parse($"[General]\nShowHiddenDevices={value}"));

        Assert.Equal(expected, settings.ShowHiddenDevices);
    }

    [Fact]
    public void Outputs_AreSortedByOrder_WithFileOrderForTies()
    {
        var settings = SettingsSerializer.Read(IniDocument.Parse("""
            [Output:c]
            Order=3
            [Output:a]
            Order=1
            [Output:x]
            [Output:b]
            Order=1
            [Output:]
            Name=ignored
            """));

        Assert.Equal(["a", "b", "c", "x"], settings.Outputs.Select(o => o.Id));
    }

    [Fact]
    public void Write_UsesListPositionAsOrder()
    {
        var settings = new AppSettings();
        settings.Outputs.Add(new OutputDeviceSettings("second"));
        settings.Outputs.Add(new OutputDeviceSettings("first"));

        var document = SettingsSerializer.Write(settings);

        Assert.Equal("1", document.GetValue("Output:second", "Order"));
        Assert.Equal("2", document.GetValue("Output:first", "Order"));
        Assert.Equal("1", document.GetValue("General", "SettingsVersion"));
    }
}
