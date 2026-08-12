using System;
using System.Diagnostics;
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

        var target = exeName.Trim('"');
        if (target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            target = target[..^4];

        var found = IntPtr.Zero;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
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

        NativeMethods.SetForegroundWindow(found);
        Thread.Sleep(150);
        return true;
    }
}
