// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows;
using System.Windows.Threading;

namespace Obevalon.AudioControlPanel.Views;

/// <summary>
/// Short on-screen message above the taskbar (SPEC F1.9): never takes focus, ignores the mouse
/// and closes itself. A new message replaces the current one.
/// </summary>
public partial class OsdWindow : Window
{
    private static OsdWindow? _current;
    private readonly DispatcherTimer _timer;

    private OsdWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Close();
        SizeChanged += (_, _) => PlaceAboveTaskbar();
        Closed += (_, _) =>
        {
            _timer.Stop();
            if (_current == this)
            {
                _current = null;
            }
        };
    }

    /// <param name="glyph">Segoe Fluent Icons glyph, e.g. "" (speaker).</param>
    public static void ShowMessage(string glyph, string message, TimeSpan duration)
    {
        var window = _current ?? new OsdWindow();
        _current = window;
        window.IconText.Text = glyph;
        window.MessageText.Text = message;
        window._timer.Stop();
        window._timer.Interval = duration;
        window._timer.Start();
        if (!window.IsVisible)
        {
            window.Show();
        }

        window.PlaceAboveTaskbar();
    }

    private void PlaceAboveTaskbar()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + ((area.Width - ActualWidth) / 2);
        Top = area.Bottom - ActualHeight - 48;
    }
}
