// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows.Interop;
using Obevalon.AudioControlPanel.Core.Hotkeys;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Platform.Hotkeys;

namespace Obevalon.AudioControlPanel.Hotkeys;

/// <summary>
/// Registers the global hotkeys from the settings on a hidden message window (SPEC §3.5)
/// and raises <see cref="HotkeyPressed"/> on the UI thread.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int TestId = 0x7FFF;
    private const int HwndMessage = -3;

    private readonly HwndSource _source;
    private readonly Dictionary<HotkeyAction, HotkeyGesture> _gestures = [];
    private readonly HashSet<HotkeyAction> _registered = [];
    private readonly HashSet<HotkeyAction> _failed = [];
    private bool _suspended;

    public HotkeyManager()
    {
        _source = new HwndSource(new HwndSourceParameters("OACP hotkeys") { ParentWindow = HwndMessage, WindowStyle = 0 });
        _source.AddHook(WndProc);
    }

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    /// <summary>Actions whose hotkey could not be registered (used by another application).</summary>
    public IReadOnlySet<HotkeyAction> Failed => _failed;

    /// <summary>Replaces all hotkeys with those in <paramref name="settings"/>.</summary>
    public void Apply(HotkeySettings settings)
    {
        UnregisterAll();
        _gestures.Clear();
        Add(HotkeyAction.NextOutput, settings.NextOutput);
        Add(HotkeyAction.ToggleMicMute, settings.ToggleMicMute);
        Add(HotkeyAction.ShowWindow, settings.ShowWindow);
        if (!_suspended)
        {
            RegisterAll();
        }
    }

    /// <summary>Temporarily releases all hotkeys (while the user records new ones).</summary>
    public void Suspend()
    {
        _suspended = true;
        UnregisterAll();
    }

    public void Resume()
    {
        _suspended = false;
        RegisterAll();
    }

    /// <summary>True when <paramref name="gesture"/> can be registered right now (no other application uses it).</summary>
    public bool IsAvailable(HotkeyGesture gesture)
    {
        if (!GlobalHotkeys.Register(_source.Handle, TestId, gesture))
        {
            return false;
        }

        GlobalHotkeys.Unregister(_source.Handle, TestId);
        return true;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    private void Add(HotkeyAction action, string? text)
    {
        if (HotkeyGesture.Parse(text) is { } gesture)
        {
            _gestures[action] = gesture;
        }
    }

    private void RegisterAll()
    {
        _failed.Clear();
        foreach (var (action, gesture) in _gestures)
        {
            if (GlobalHotkeys.Register(_source.Handle, (int)action + 1, gesture))
            {
                _registered.Add(action);
            }
            else
            {
                _failed.Add(action);
            }
        }
    }

    private void UnregisterAll()
    {
        foreach (var action in _registered)
        {
            GlobalHotkeys.Unregister(_source.Handle, (int)action + 1);
        }

        _registered.Clear();
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == GlobalHotkeys.WmHotkey)
        {
            var action = (HotkeyAction)((int)wParam - 1);
            if (_registered.Contains(action))
            {
                HotkeyPressed?.Invoke(this, action);
                handled = true;
            }
        }

        return 0;
    }
}
