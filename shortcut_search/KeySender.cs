using System.Collections.Generic;
using Windows.UI.Input.Preview.Injection;

namespace shortcut_search;

internal static class KeySender
{
    private const ushort VK_L = 0x4C;

    /// <summary>
    /// Sends the shortcut via the WinRT input-injection broker. Returns
    /// true on success, false if the injector couldn't be created (e.g.
    /// the inputInjectionBrokered capability is missing from the manifest).
    /// </summary>
    public static bool Send(ShortcutEntry entry)
    {
        // Windows refuses to let synthetic input trigger the lock screen,
        // regardless of injection method. LockWorkStation() is the only
        // way to do this programmatically.
        if (IsLockCombo(entry))
            return NativeMethods.LockWorkStation();

        var injector = InputInjector.TryCreate();
        if (injector is null)
            return false;

        var mods = new List<ushort>();
        if (entry.Ctrl) mods.Add(VirtualKeys.VK_CONTROL);
        if (entry.Alt) mods.Add(VirtualKeys.VK_MENU);
        if (entry.Shift) mods.Add(VirtualKeys.VK_SHIFT);
        if (entry.Win) mods.Add(VirtualKeys.VK_LWIN);

        var events = new List<InjectedInputKeyboardInfo>();

        foreach (var vk in mods)
            events.Add(KeyEvent(vk, down: true));
        foreach (var vk in entry.Keys)
            events.Add(KeyEvent(vk, down: true));

        for (var idx = entry.Keys.Length - 1; idx >= 0; idx--)
            events.Add(KeyEvent(entry.Keys[idx], down: false));
        for (var idx = mods.Count - 1; idx >= 0; idx--)
            events.Add(KeyEvent(mods[idx], down: false));

        if (events.Count == 0)
            return false;

        injector.InjectKeyboardInput(events);
        return true;
    }

    private static bool IsLockCombo(ShortcutEntry entry) =>
        entry.Win && !entry.Ctrl && !entry.Alt && !entry.Shift
        && entry.Keys.Length == 1 && entry.Keys[0] == VK_L;

    private static bool IsExtendedKey(ushort vk) => vk switch
    {
        0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2C or 0x2D or 0x2E or 0x5B or 0x5C or 0x5D or 0x6F or 0x90 => true,
        _ => false,
    };

    private static InjectedInputKeyboardInfo KeyEvent(ushort vk, bool down)
    {
        var options = InjectedInputKeyOptions.None;
        if (!down)
        {
            options |= InjectedInputKeyOptions.KeyUp;
        }

        if (IsExtendedKey(vk))
        {
            options |= InjectedInputKeyOptions.ExtendedKey;
        }

        return new InjectedInputKeyboardInfo
        {
            VirtualKey = vk,
            KeyOptions = options,
        };
    }
}
