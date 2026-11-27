// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Platform.Shell;

/// <summary>
/// Ensures one running instance per Windows session (SPEC F3.7). A second start signals the
/// running instance to show its window and exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string BaseName = @"Local\Obevalon.AudioControlPanel";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly RegisteredWaitHandle _registration;

    private SingleInstance(Mutex mutex, EventWaitHandle activateEvent, Action onActivationRequested)
    {
        _mutex = mutex;
        _activateEvent = activateEvent;
        _registration = ThreadPool.RegisterWaitForSingleObject(
            activateEvent, (_, _) => onActivationRequested(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    /// <summary>
    /// Becomes the single instance, waiting up to <paramref name="timeout"/> for a previous instance
    /// to exit (used after an update). Returns null when another instance keeps running.
    /// </summary>
    /// <param name="onActivationRequested">Called on a thread-pool thread when another start asks to show the window.</param>
    public static SingleInstance? TryAcquire(TimeSpan timeout, Action onActivationRequested)
    {
        ArgumentNullException.ThrowIfNull(onActivationRequested);

        var mutex = new Mutex(initiallyOwned: false, BaseName + ".Mutex");
        bool acquired;
        try
        {
            acquired = mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance crashed; the mutex is ours now.
            acquired = true;
        }

        if (!acquired)
        {
            mutex.Dispose();
            return null;
        }

        var activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, BaseName + ".Activate");
        return new SingleInstance(mutex, activateEvent, onActivationRequested);
    }

    /// <summary>Asks the running instance to show its window.</summary>
    public static void SignalRunningInstance()
    {
        if (EventWaitHandle.TryOpenExisting(BaseName + ".Activate", out var activateEvent))
        {
            using (activateEvent)
            {
                activateEvent.Set();
            }
        }
    }

    public void Dispose()
    {
        _registration.Unregister(null);
        _activateEvent.Dispose();
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Not owned by this thread (should not happen); the OS releases it on exit.
        }

        _mutex.Dispose();
    }
}
