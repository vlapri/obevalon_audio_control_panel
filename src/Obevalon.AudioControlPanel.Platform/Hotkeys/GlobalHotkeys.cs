// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using Obevalon.AudioControlPanel.Core.Hotkeys;

namespace Obevalon.AudioControlPanel.Platform.Hotkeys;

/// <summary>Win32 RegisterHotKey / UnregisterHotKey. The window receives WM_HOTKEY with the hotkey ID.</summary>
public static partial class GlobalHotkeys
{
    public const int WmHotkey = 0x0312;

    private const uint ModNoRepeat = 0x4000;

    /// <summary>Registers a hotkey; returns false when another application already uses it.</summary>
    public static bool Register(nint windowHandle, int id, HotkeyGesture gesture)
    {
        ArgumentNullException.ThrowIfNull(gesture);
        return RegisterHotKey(windowHandle, id, (uint)gesture.Modifiers | ModNoRepeat, (uint)gesture.VirtualKey);
    }

    public static void Unregister(nint windowHandle, int id) => UnregisterHotKey(windowHandle, id);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(nint windowHandle, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint windowHandle, int id);
}
