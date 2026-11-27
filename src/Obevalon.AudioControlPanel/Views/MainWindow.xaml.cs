// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Obevalon.AudioControlPanel.ViewModels;

namespace Obevalon.AudioControlPanel.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Restores a saved position when it is still on one of the screens.</summary>
    public void RestorePosition(double? left, double? top)
    {
        if (left is not { } x || top is not { } y)
        {
            return;
        }

        // Keep at least the title bar reachable (the screen setup may have changed since).
        var minX = SystemParameters.VirtualScreenLeft - (Width / 2);
        var maxX = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100;
        var minY = SystemParameters.VirtualScreenTop;
        var maxY = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60;
        if (x < minX || x > maxX || y < minY || y > maxY)
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = x;
        Top = y;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (DataContext is MainViewModel viewModel)
        {
            var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
            if (!bounds.IsEmpty)
            {
                viewModel.SaveWindowPosition(bounds.Left, bounds.Top);
            }
        }
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
