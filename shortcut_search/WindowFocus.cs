using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace shortcut_search;

internal static class WindowFocus
{
    /// <summary>
    /// Tries to bring a window belonging to the given process name (e.g.
    /// "notepad.exe" or "Notepad") to the foreground. Returns true if a
    /// matching window was found and focused.
    /// </summary>
    public static bool FocusByProcessName(string exeName)
    {
        if (string.IsNullOrWhiteSpace(exeName))
            return false;

        var target = exeName.Trim('"', ' ');
        target = Path.GetFileNameWithoutExtension(target);

        var found = IntPtr.Zero;

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            _ = NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                if (string.Equals(proc.ProcessName, target, StringComparison.OrdinalIgnoreCase))
                {
                    found = hWnd;
                    return false; // stop enumerating
                }
            }
            catch
            {
                // Process may have exited between EnumWindows and GetProcessById.
            }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
            return false;

        if (NativeMethods.IsIconic(found))
        {
            NativeMethods.ShowWindow(found, 9 /* SW_RESTORE */);
        }

        NativeMethods.SetForegroundWindow(found);
        Thread.Sleep(150);
        return true;
    }
}
