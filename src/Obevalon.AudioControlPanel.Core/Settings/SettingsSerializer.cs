// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Obevalon.AudioControlPanel.Core.Localization;

namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>
/// Converts <see cref="AppSettings"/> to and from <see cref="IniDocument"/>.
/// Reading never throws: missing or invalid values keep their defaults.
/// </summary>
public static class SettingsSerializer
{
    public const string GeneralSection = "General";
    public const string MicrophoneSection = "Microphone";
    public const string HotkeysSection = "Hotkeys";
    public const string WindowSection = "Window";
    public const string UpdatesSection = "Updates";
    public const string OutputSectionPrefix = "Output:";

    public static AppSettings Read(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        SettingsMigrator.Migrate(document);

        var settings = new AppSettings();
        var general = document.GetSection(GeneralSection);
        settings.SettingsVersion = GetInt(general, "SettingsVersion") ?? AppSettings.CurrentSettingsVersion;
        settings.Language = NormalizeLanguage(GetToken(general, "Language"));
        settings.Theme = GetEnum(general, "Theme", settings.Theme);
        settings.CloseToTray = GetBool(general, "CloseToTray") ?? settings.CloseToTray;
        settings.SwitchCommunicationsDevice = GetBool(general, "SwitchCommunicationsDevice") ?? settings.SwitchCommunicationsDevice;
        settings.ShowSwitchOsd = GetBool(general, "ShowSwitchOsd") ?? settings.ShowSwitchOsd;
        settings.ShowHiddenDevices = GetBool(general, "ShowHiddenDevices") ?? settings.ShowHiddenDevices;
        settings.Logging = GetBool(general, "Logging") ?? settings.Logging;

        settings.MicrophoneDeviceId = GetString(document.GetSection(MicrophoneSection), "DeviceId");

        var hotkeys = document.GetSection(HotkeysSection);
        settings.Hotkeys.NextOutput = GetString(hotkeys, "NextOutput");
        settings.Hotkeys.ToggleMicMute = GetString(hotkeys, "ToggleMicMute");
        settings.Hotkeys.ShowWindow = GetString(hotkeys, "ShowWindow");

        var window = document.GetSection(WindowSection);
        settings.WindowLeft = GetDouble(window, "Left");
        settings.WindowTop = GetDouble(window, "Top");

        var updates = document.GetSection(UpdatesSection);
        settings.Updates.AutoCheck = GetBool(updates, "AutoCheck") ?? settings.Updates.AutoCheck;
        settings.Updates.LastCheckUtc = GetDateTime(updates, "LastCheckUtc");
        settings.Updates.SkippedVersion = GetString(updates, "SkippedVersion");
        settings.Updates.ManifestUrl = GetString(updates, "ManifestUrl");

        ReadOutputs(document, settings);
        return settings;
    }

    public static IniDocument Write(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var document = new IniDocument();
        document.HeaderComments.Add($"{AppInfo.ProductName} settings");
        document.HeaderComments.Add("The file is rewritten by the application; comments are not kept.");

        var general = document.GetOrAddSection(GeneralSection);
        general.SetValue("SettingsVersion", Format(AppSettings.CurrentSettingsVersion));
        general.SetValue("Language", settings.Language, "auto | cs | en");
        general.SetValue("Theme", settings.Theme.ToString().ToLowerInvariant(), "auto | light | dark");
        general.SetValue("CloseToTray", Format(settings.CloseToTray));
        general.SetValue("SwitchCommunicationsDevice", Format(settings.SwitchCommunicationsDevice));
        general.SetValue("ShowSwitchOsd", Format(settings.ShowSwitchOsd));
        general.SetValue("ShowHiddenDevices", Format(settings.ShowHiddenDevices));
        general.SetValue("Logging", Format(settings.Logging), "diagnostic log in the logs folder");

        document.GetOrAddSection(MicrophoneSection)
            .SetValue("DeviceId", settings.MicrophoneDeviceId, "empty = system default microphone");

        var hotkeys = document.GetOrAddSection(HotkeysSection);
        hotkeys.Comment = "e.g. Ctrl+Alt+A; empty = not assigned";
        hotkeys.SetValue("NextOutput", settings.Hotkeys.NextOutput);
        hotkeys.SetValue("ToggleMicMute", settings.Hotkeys.ToggleMicMute);
        hotkeys.SetValue("ShowWindow", settings.Hotkeys.ShowWindow);

        var window = document.GetOrAddSection(WindowSection);
        window.SetValue("Left", Format(settings.WindowLeft));
        window.SetValue("Top", Format(settings.WindowTop));

        var updates = document.GetOrAddSection(UpdatesSection);
        updates.SetValue("AutoCheck", Format(settings.Updates.AutoCheck));
        updates.SetValue("LastCheckUtc", settings.Updates.LastCheckUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        updates.SetValue("SkippedVersion", settings.Updates.SkippedVersion);
        if (!string.IsNullOrEmpty(settings.Updates.ManifestUrl))
        {
            updates.SetValue("ManifestUrl", settings.Updates.ManifestUrl);
        }

        for (var i = 0; i < settings.Outputs.Count; i++)
        {
            var output = settings.Outputs[i];
            var section = document.GetOrAddSection(OutputSectionPrefix + output.Id);
            if (i == 0)
            {
                section.Comment = "One section per known audio output, Order = position in the list";
            }

            section.SetValue("SystemName", output.SystemName);
            section.SetValue("Name", output.Name);
            section.SetValue("Note", output.Note);
            section.SetValue("Order", Format(i + 1));
            section.SetValue("InCycle", Format(output.InCycle));
            section.SetValue("Hidden", Format(output.Hidden));
        }

        return document;
    }

    private static void ReadOutputs(IniDocument document, AppSettings settings)
    {
        var outputs = new List<(int Order, int Index, OutputDeviceSettings Output)>();
        var index = 0;
        foreach (var section in document.Sections)
        {
            if (!section.Name.StartsWith(OutputSectionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = section.Name[OutputSectionPrefix.Length..].Trim();
            if (id.Length == 0 || outputs.Exists(o => string.Equals(o.Output.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var output = new OutputDeviceSettings(id)
            {
                SystemName = GetString(section, "SystemName"),
                Name = GetString(section, "Name"),
                Note = GetString(section, "Note"),
                InCycle = GetBool(section, "InCycle") ?? false,
                Hidden = GetBool(section, "Hidden") ?? false,
            };
            outputs.Add((GetInt(section, "Order") ?? int.MaxValue, index++, output));
        }

        // Sort by Order; ties and missing values keep the file order.
        settings.Outputs.AddRange(outputs.OrderBy(o => o.Order).ThenBy(o => o.Index).Select(o => o.Output));
    }

    private static string NormalizeLanguage(string? value) =>
        value?.ToLowerInvariant() switch
        {
            LanguageResolver.Czech => LanguageResolver.Czech,
            LanguageResolver.English => LanguageResolver.English,
            _ => LanguageResolver.Auto,
        };

    private static string? GetString(IniSection? section, string key)
    {
        var value = section?.GetValue(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Value for non-text settings: the first token, ignoring an inline "; comment".</summary>
    private static string? GetToken(IniSection? section, string key)
    {
        var value = GetString(section, key);
        if (value is null)
        {
            return null;
        }

        var comment = value.IndexOfAny([';', '#']);
        var token = (comment >= 0 ? value[..comment] : value).Trim();
        return token.Length == 0 ? null : token;
    }

    private static bool? GetBool(IniSection? section, string key) =>
        GetToken(section, key)?.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => null,
        };

    private static int? GetInt(IniSection? section, string key) =>
        int.TryParse(GetToken(section, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static double? GetDouble(IniSection? section, string key) =>
        double.TryParse(GetToken(section, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value) ? value : null;

    private static DateTimeOffset? GetDateTime(IniSection? section, string key) =>
        DateTimeOffset.TryParse(GetToken(section, key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value.ToUniversalTime() : null;

    private static TEnum GetEnum<TEnum>(IniSection? section, string key, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(GetToken(section, key), ignoreCase: true, out var value)
            && Enum.IsDefined(value)
            && !int.TryParse(GetToken(section, key), out _)
            ? value : fallback;

    private static string Format(bool value) => value ? "true" : "false";

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(double? value) => value?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty;
}
