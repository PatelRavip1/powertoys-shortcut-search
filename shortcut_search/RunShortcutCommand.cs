using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace shortcut_search;

internal sealed partial class RunShortcutCommand : InvokableCommand
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "shortcut_search_log.txt");

    private readonly ShortcutEntry _entry;

    public RunShortcutCommand(ShortcutEntry entry)
    {
        _entry = entry;
        Name = "Run";
        Icon = new IconInfo("\uE945"); // lightning bolt
    }

    private static bool NeedsFocus(ShortcutEntry entry) =>
        entry.Category == ShortcutCategory.App
        && !string.IsNullOrWhiteSpace(entry.WindowFilter)
        && entry.WindowFilter.Trim() != "*";

    public override CommandResult Invoke()
    {
        var entry = _entry;
        Log($"Invoke() called for {entry.App} -> {entry.ActionName} ({entry.Display}), Category={entry.Category}, WindowFilter='{entry.WindowFilter}'");

        Task.Run(() =>
        {
            try
            {
                Log("Background task started, sleeping 300ms");
                Thread.Sleep(300);

                var focused = false;
                if (NeedsFocus(entry))
                {
                    Log($"Specific target app: '{entry.WindowFilter}', attempting focus");
                    focused = WindowFocus.FocusByProcessName(entry.WindowFilter);
                    Log($"Focus result: {focused}");
                    if (!focused)
                    {
                        TryToast($"Couldn't find window for '{entry.WindowFilter}'");
                        return;
                    }
                }
                else
                {
                    Log("Global shortcut (Windows/PowerToys or wildcard), sending directly");
                }

                Log($"Calling KeySender.Send (InputInjector): Ctrl={entry.Ctrl} Alt={entry.Alt} Shift={entry.Shift} Win={entry.Win} Keys=[{string.Join(",", entry.Keys)}]");
                var ok = KeySender.Send(entry);
                Log($"KeySender.Send returned: {ok}");

                TryToast(ok
                    ? $"Sent {entry.Display}"
                    : "InputInjector unavailable — check the inputInjectionBrokered capability in Package.appxmanifest");
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION: {ex}");
                TryToast($"Error: {ex.Message}");
            }
        });

        return CommandResult.Hide();
    }

    private static void TryToast(string message)
    {
        try
        {
            new ToastStatusMessage(message).Show();
        }
        catch (Exception ex)
        {
            Log($"Toast itself threw: {ex}");
        }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // If we can't even log, there's nothing more we can do.
        }
    }
}
