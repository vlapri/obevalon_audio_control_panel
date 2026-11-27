// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Diagnostics;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Core.Updates;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Platform.Updates;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>State of the update notification (SPEC §3.9).</summary>
public enum UpdateState
{
    Idle,
    Checking,
    Available,
    Downloading,
    Installing,
}

/// <summary>
/// Automatic and manual update checks, the update notification, download, verification and install.
/// Nothing is downloaded or installed without the user's consent (SPEC F9.4).
/// </summary>
public sealed partial class UpdateViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly SettingsManager _settings;
    private readonly HttpClient _http;
    private readonly ExeSwapUpdateInstaller _installer;
    private readonly Func<bool> _isMainWindowVisible;
    private readonly Action _exitApplication;
    private readonly DispatcherTimer _timer;
    private readonly SemanticVersion _currentVersion;
    private UpdateCheckResult? _result;
    private CancellationTokenSource? _downloadCancellation;

    public UpdateViewModel(SettingsManager settings, string executablePath, Func<bool> isMainWindowVisible, Action exitApplication)
    {
        _settings = settings;
        _isMainWindowVisible = isMainWindowVisible;
        _exitApplication = exitApplication;
        _installer = new ExeSwapUpdateInstaller(executablePath);
        _currentVersion = SemanticVersion.Parse(AppInfo.Version) ?? SemanticVersion.Parse("0.0.0-dev")!;

        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.ShortName}/{_currentVersion}");

        _timer = new DispatcherTimer { Interval = FirstCheckDelay };
        _timer.Tick += OnTimerTick;
    }

    /// <summary>Raised when an automatic or manual check finds an update the user should see.</summary>
    public event EventHandler? UpdateFound;

    /// <summary>Raised when the notification should close (Later, Skip, install started).</summary>
    public event EventHandler? CloseRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateAvailable), nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand), nameof(CheckNowCommand))]
    private UpdateState _state;

    /// <summary>The version offered to the user, e.g. "1.2.0".</summary>
    [ObservableProperty]
    private string? _availableVersion;

    [ObservableProperty]
    private double _progress;

    /// <summary>Result or error text shown in the notification and the settings window.</summary>
    [ObservableProperty]
    private string? _message;

    /// <summary>Offer a link to the release page (read-only folder) or to the .NET download.</summary>
    [ObservableProperty]
    private string? _manualDownloadUrl;

    public bool IsUpdateAvailable => State is UpdateState.Available or UpdateState.Downloading or UpdateState.Installing;

    public bool IsBusy => State is UpdateState.Checking or UpdateState.Downloading or UpdateState.Installing;

    public string CurrentVersion => _currentVersion.ToString();

    public string? LastCheckText
    {
        get
        {
            var loc = LocalizationManager.Instance;
            return _settings.Current.Updates.LastCheckUtc is { } last
                ? loc.Format("Update_LastCheck", last.ToLocalTime().ToString("g", loc.Culture))
                : loc["Update_Never"];
        }
    }

    /// <summary>
    /// Starts automatic checks: shortly after start, then every 24 hours (SPEC F9.1).
    /// Local and CI builds (0.0.0-dev, 0.0.0-ci.N) never check automatically.
    /// </summary>
    public void StartAutomaticChecks()
    {
        if (_currentVersion.Major == 0 && _currentVersion.Minor == 0 && _currentVersion.Patch == 0)
        {
            return;
        }

        _timer.Interval = FirstCheckDelay;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _downloadCancellation?.Cancel();
        _downloadCancellation?.Dispose();
        _http.Dispose();
    }

    private bool CanCheckNow() => !IsBusy;

    /// <summary>Manual check from the settings or the about window (always checks, shows the result).</summary>
    [RelayCommand(CanExecute = nameof(CanCheckNow))]
    private Task CheckNow() => CheckAsync(manual: true);

    private bool CanInstall() => State == UpdateState.Available && _result?.Asset is not null;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task Install()
    {
        var loc = LocalizationManager.Instance;
        if (_result is not { Asset: { } asset } result)
        {
            return;
        }

        if (result.Status == UpdateCheckStatus.RuntimeRequired)
        {
            var major = SemanticVersion.Parse(result.Manifest?.MinimumRuntime)?.Major ?? 10;
            Message = loc.Format("Update_RuntimeRequired", result.Manifest?.MinimumRuntime);
            ManualDownloadUrl = $"https://dotnet.microsoft.com/download/dotnet/{major}.0";
            return;
        }

        if (!_installer.CanInstall)
        {
            Message = loc["Update_FolderNotWritable"];
            ManualDownloadUrl = result.Manifest?.ReleaseNotesUrl ?? AppInfo.ReleasesUrl;
            return;
        }

        State = UpdateState.Downloading;
        Message = loc["Update_Downloading"];
        ManualDownloadUrl = null;
        Progress = 0;
        _downloadCancellation = new CancellationTokenSource();
        try
        {
            var directory = Path.Combine(Path.GetTempPath(), "Obevalon", "AudioControlPanel", "update");
            var progress = new Progress<double>(value => Progress = value * 100);
            var downloaded = await new UpdateDownloader(_http)
                .DownloadAsync(asset, directory, progress, _downloadCancellation.Token)
                .ConfigureAwait(true);

            State = UpdateState.Installing;
            Message = loc["Update_Installing"];
            AppLog.Info($"Installing update {AvailableVersion}");
            _installer.InstallAndRestart(downloaded, startInTray: !_isMainWindowVisible());
            CloseRequested?.Invoke(this, EventArgs.Empty);
            _exitApplication();
        }
        catch (UpdateVerificationException ex)
        {
            AppLog.Error("Update verification failed", ex);
            State = UpdateState.Available;
            Message = loc["Update_VerificationFailed"];
        }
        catch (OperationCanceledException)
        {
            State = UpdateState.Available;
            Message = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            AppLog.Error("Update failed", ex);
            State = UpdateState.Available;
            Message = loc.Format("Update_Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void Later()
    {
        _downloadCancellation?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Do not offer this version again (a newer one will be offered).</summary>
    [RelayCommand]
    private void Skip()
    {
        _settings.Current.Updates.SkippedVersion = AvailableVersion;
        _settings.Save();
        State = UpdateState.Idle;
        AvailableVersion = null;
        _result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenReleaseNotes() => OpenUrl(_result?.Manifest?.ReleaseNotesUrl ?? AppInfo.ReleasesUrl);

    [RelayCommand]
    private void OpenManualDownload()
    {
        if (ManualDownloadUrl is { } url)
        {
            OpenUrl(url);
        }
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Interval = CheckInterval;
        if (_settings.Current.Updates.AutoCheck && !IsBusy && State != UpdateState.Available)
        {
            await CheckAsync(manual: false).ConfigureAwait(true);
        }
    }

    private async Task CheckAsync(bool manual)
    {
        var loc = LocalizationManager.Instance;
        var previousState = State;
        State = UpdateState.Checking;
        if (manual)
        {
            Message = loc["Update_Checking"];
        }

        UpdateCheckResult result;
        try
        {
            var manifestUri = Uri.TryCreate(_settings.Current.Updates.ManifestUrl, UriKind.Absolute, out var custom)
                ? custom
                : UpdateChecker.DefaultManifestUri;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var installedRuntimes = DotNetRuntimeProbe.GetInstalledDesktopRuntimes();
            result = await new UpdateChecker(_http, UpdateSignature.TrustedPublicKeys).CheckAsync(
                manifestUri,
                _currentVersion,
                DotNetRuntimeProbe.RuntimeIdentifier,
                _settings.Current.Updates.SkippedVersion,
                manual,
                minimum => RuntimeRequirement.IsSatisfied(minimum, installedRuntimes),
                timeout.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException ex)
        {
            result = new UpdateCheckResult(UpdateCheckStatus.NetworkError, Error: ex.Message);
        }

        AppLog.Info($"Update check ({(manual ? "manual" : "automatic")}): {result.Status} {result.Version} {result.Error}");
        if (result.Status is not (UpdateCheckStatus.NetworkError or UpdateCheckStatus.NotConfigured))
        {
            _settings.Current.Updates.LastCheckUtc = DateTimeOffset.UtcNow;
            _settings.Save();
            OnPropertyChanged(nameof(LastCheckText));
        }

        if (result.Status is UpdateCheckStatus.Available or UpdateCheckStatus.RuntimeRequired)
        {
            _result = result;
            AvailableVersion = result.Version?.ToString();
            State = UpdateState.Available;
            Message = result.Status == UpdateCheckStatus.RuntimeRequired
                ? loc.Format("Update_RuntimeRequired", result.Manifest?.MinimumRuntime)
                : null;
            ManualDownloadUrl = null;
            UpdateFound?.Invoke(this, EventArgs.Empty);
            return;
        }

        State = previousState == UpdateState.Available ? UpdateState.Available : UpdateState.Idle;

        // Automatic checks stay silent (SPEC F9.12); manual checks show the result.
        if (manual)
        {
            Message = result.Status switch
            {
                UpdateCheckStatus.UpToDate => loc["Update_UpToDate"],
                UpdateCheckStatus.NotConfigured => loc["Update_NotConfigured"],
                UpdateCheckStatus.InvalidSignature => loc["Update_InvalidSignature"],
                UpdateCheckStatus.InvalidManifest => loc["Update_InvalidManifest"],
                UpdateCheckStatus.NoAssetForPlatform => loc["Update_NoAsset"],
                _ => loc.Format("Update_CheckFailed", result.Error),
            };
        }
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
