// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Platform.Native;

namespace Obevalon.AudioControlPanel.Platform.Shell;

public static class SystemMetrics
{
    /// <summary>Size of small icons (tray icons) in physical pixels, e.g. 16, 20, 24 or 32.</summary>
    public static int SmallIconSize
    {
        get
        {
            var size = User32.GetSystemMetrics(User32.SmCxSmIcon);
            return size > 0 ? size : 16;
        }
    }
}
