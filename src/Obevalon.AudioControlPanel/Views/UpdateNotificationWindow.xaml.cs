// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows;

namespace Obevalon.AudioControlPanel.Views;

/// <summary>Unobtrusive update notification in the bottom-right corner (SPEC F9.3, not a Windows toast).</summary>
public partial class UpdateNotificationWindow : Window
{
    public UpdateNotificationWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PlaceInCorner(this);
    }

    /// <summary>Places a window in the bottom-right corner of the primary work area.</summary>
    internal static void PlaceInCorner(Window window)
    {
        var area = SystemParameters.WorkArea;
        window.Left = area.Right - window.ActualWidth - 16;
        window.Top = area.Bottom - window.ActualHeight - 16;
    }
}
