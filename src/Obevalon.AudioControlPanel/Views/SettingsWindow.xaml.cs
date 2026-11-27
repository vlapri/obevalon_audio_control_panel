// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows;

namespace Obevalon.AudioControlPanel.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
