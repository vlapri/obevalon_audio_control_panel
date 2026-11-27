// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>Holds the current settings and saves them without throwing.</summary>
public sealed class SettingsManager
{
    private readonly ISettingsStore _store;

    public SettingsManager(ISettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        Current = store.Load();
    }

    public AppSettings Current { get; }

    public string SettingsPath => _store.SettingsPath;

    public bool IsPortable => _store.IsPortable;

    /// <summary>The error of the last failed save, or null when the last save succeeded.</summary>
    public Exception? LastSaveError { get; private set; }

    /// <summary>Saves the current settings; returns false (and keeps the error) on failure.</summary>
    public bool Save()
    {
        try
        {
            _store.Save(Current);
            LastSaveError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LastSaveError = ex;
            return false;
        }
    }
}
