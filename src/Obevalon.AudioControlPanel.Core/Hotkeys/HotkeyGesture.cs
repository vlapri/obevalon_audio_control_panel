// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Obevalon.AudioControlPanel.Core.Hotkeys;

/// <summary>Modifier keys of a global hotkey (values match Win32 MOD_* flags).</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Windows = 0x8,
}

/// <summary>Actions that can have a global hotkey (SPEC F5.1).</summary>
public enum HotkeyAction
{
    NextOutput,
    ToggleMicMute,
    ShowWindow,
}

/// <summary>
/// A global hotkey: modifiers plus a Windows virtual-key code, written as text like "Ctrl+Alt+A".
/// </summary>
public sealed record HotkeyGesture(HotkeyModifiers Modifiers, int VirtualKey)
{
    private static readonly (HotkeyModifiers Modifier, string Name)[] ModifierNames =
    [
        (HotkeyModifiers.Control, "Ctrl"),
        (HotkeyModifiers.Alt, "Alt"),
        (HotkeyModifiers.Shift, "Shift"),
        (HotkeyModifiers.Windows, "Win"),
    ];

    private static readonly Dictionary<int, string> KeyNames = BuildKeyNames();

    private static readonly Dictionary<string, int> KeysByName =
        KeyNames.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Name of the key alone, e.g. "A", "F5", "Space"; null for an unsupported key.</summary>
    public static string? GetKeyName(int virtualKey) => KeyNames.GetValueOrDefault(virtualKey);

    /// <summary>
    /// A hotkey must use Ctrl, Alt or Win (Shift alone would block normal typing),
    /// except for F13–F24 which keyboards rarely use for anything else.
    /// </summary>
    public bool IsValid =>
        KeyNames.ContainsKey(VirtualKey)
        && ((Modifiers & (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Windows)) != 0
            || VirtualKey is >= 0x7C and <= 0x87);

    public override string ToString()
    {
        var parts = ModifierNames.Where(m => Modifiers.HasFlag(m.Modifier)).Select(m => m.Name).ToList();
        parts.Add(GetKeyName(VirtualKey) ?? $"0x{VirtualKey:X2}");
        return string.Join('+', parts);
    }

    /// <summary>Parses text like "Ctrl+Alt+A"; returns null for empty, unknown or invalid input.</summary>
    public static HotkeyGesture? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var modifiers = HotkeyModifiers.None;
        int? key = null;
        foreach (var rawPart in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var modifier = rawPart.ToLowerInvariant() switch
            {
                "ctrl" or "control" => HotkeyModifiers.Control,
                "alt" => HotkeyModifiers.Alt,
                "shift" => HotkeyModifiers.Shift,
                "win" or "windows" => HotkeyModifiers.Windows,
                _ => HotkeyModifiers.None,
            };

            if (modifier != HotkeyModifiers.None)
            {
                modifiers |= modifier;
            }
            else if (key is null && KeysByName.TryGetValue(rawPart, out var virtualKey))
            {
                key = virtualKey;
            }
            else
            {
                return null;
            }
        }

        if (key is null)
        {
            return null;
        }

        var gesture = new HotkeyGesture(modifiers, key.Value);
        return gesture.IsValid ? gesture : null;
    }

    private static Dictionary<int, string> BuildKeyNames()
    {
        var names = new Dictionary<int, string>();
        for (var c = 'A'; c <= 'Z'; c++)
        {
            names[c] = c.ToString();
        }

        for (var d = 0; d <= 9; d++)
        {
            names[0x30 + d] = d.ToString(CultureInfo.InvariantCulture);
            names[0x60 + d] = "Num" + d.ToString(CultureInfo.InvariantCulture);
        }

        for (var f = 1; f <= 24; f++)
        {
            names[0x6F + f] = "F" + f.ToString(CultureInfo.InvariantCulture);
        }

        names[0x08] = "Backspace";
        names[0x09] = "Tab";
        names[0x0D] = "Enter";
        names[0x13] = "Pause";
        names[0x20] = "Space";
        names[0x21] = "PageUp";
        names[0x22] = "PageDown";
        names[0x23] = "End";
        names[0x24] = "Home";
        names[0x25] = "Left";
        names[0x26] = "Up";
        names[0x27] = "Right";
        names[0x28] = "Down";
        names[0x2D] = "Insert";
        names[0x2E] = "Delete";
        names[0x6A] = "Multiply";
        names[0x6B] = "Add";
        names[0x6D] = "Subtract";
        names[0x6E] = "Decimal";
        names[0x6F] = "Divide";
        names[0xAD] = "VolumeMute";
        names[0xAE] = "VolumeDown";
        names[0xAF] = "VolumeUp";
        names[0xB0] = "MediaNext";
        names[0xB1] = "MediaPrevious";
        names[0xB2] = "MediaStop";
        names[0xB3] = "MediaPlayPause";
        names[0xBA] = "Oem1";
        names[0xBB] = "OemPlus";
        names[0xBC] = "OemComma";
        names[0xBD] = "OemMinus";
        names[0xBE] = "OemPeriod";
        names[0xBF] = "Oem2";
        names[0xC0] = "Oem3";
        names[0xDB] = "Oem4";
        names[0xDC] = "Oem5";
        names[0xDD] = "Oem6";
        names[0xDE] = "Oem7";
        return names;
    }
}
