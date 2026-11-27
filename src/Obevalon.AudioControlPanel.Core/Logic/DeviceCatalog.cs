// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Logic;

/// <summary>Merges the system's audio outputs with the user's settings.</summary>
public static class DeviceCatalog
{
    /// <summary>
    /// Adds newly seen devices to <paramref name="settings"/> (at the end, available ones first),
    /// refreshes their last known system names and returns all outputs in the user's order.
    /// </summary>
    /// <param name="settingsChanged">True when <paramref name="settings"/> was modified and should be saved.</param>
    public static IReadOnlyList<OutputEntry> Merge(
        AppSettings settings,
        IReadOnlyList<AudioDevice> systemDevices,
        string? defaultDeviceId,
        out bool settingsChanged)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(systemDevices);

        settingsChanged = false;
        var byId = new Dictionary<string, AudioDevice>(StringComparer.OrdinalIgnoreCase);
        foreach (var device in systemDevices)
        {
            byId.TryAdd(device.Id, device);
        }

        // New devices go to the end: available ones first, then by name.
        var newDevices = byId.Values
            .Where(d => settings.FindOutput(d.Id) is null)
            .OrderBy(d => d.IsAvailable ? 0 : 1)
            .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase);
        foreach (var device in newDevices)
        {
            settings.Outputs.Add(new OutputDeviceSettings(device.Id) { SystemName = device.Name });
            settingsChanged = true;
        }

        var entries = new List<OutputEntry>(settings.Outputs.Count);
        foreach (var output in settings.Outputs)
        {
            byId.TryGetValue(output.Id, out var device);
            if (device is not null && device.Name != output.SystemName)
            {
                output.SystemName = device.Name;
                settingsChanged = true;
            }

            entries.Add(new OutputEntry(
                output.Id,
                device?.Name ?? output.SystemName ?? output.Id,
                output.Name,
                output.Note,
                output.InCycle,
                output.Hidden,
                device?.State,
                defaultDeviceId is not null && string.Equals(output.Id, defaultDeviceId, StringComparison.OrdinalIgnoreCase)));
        }

        return entries;
    }

    /// <summary>
    /// Moves an output up (<paramref name="offset"/> &lt; 0) or down in the user's order.
    /// Returns false when it is already at the edge or not found.
    /// </summary>
    public static bool Move(AppSettings settings, string deviceId, int offset)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var index = settings.Outputs.FindIndex(o => string.Equals(o.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        var target = index + offset;
        if (index < 0 || offset == 0 || target < 0 || target >= settings.Outputs.Count)
        {
            return false;
        }

        var output = settings.Outputs[index];
        settings.Outputs.RemoveAt(index);
        settings.Outputs.Insert(target, output);
        return true;
    }

    /// <summary>Removes a device from the settings (used for missing devices).</summary>
    public static bool Remove(AppSettings settings, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Outputs.RemoveAll(o => string.Equals(o.Id, deviceId, StringComparison.OrdinalIgnoreCase)) > 0;
    }
}
