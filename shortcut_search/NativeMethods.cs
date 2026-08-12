using System;
using System.Runtime.InteropServices;

namespace shortcut_search;

internal static class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // Windows deliberately blocks synthetic/injected input from triggering
    // the lock screen (Win+L). Locking has to go through this dedicated API.
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool LockWorkStation();
}
