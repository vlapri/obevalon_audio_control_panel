// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;

namespace Obevalon.AudioControlPanel.Platform.Native;

internal static partial class User32
{
    public const int SmCxSmIcon = 49;

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int index);
}
