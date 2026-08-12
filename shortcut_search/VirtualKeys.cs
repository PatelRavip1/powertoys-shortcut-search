using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace shortcut_search;

// Resolves a manifest key token (a raw Virtual-Key int, as PowerToys' own
// manifest uses, or a string name like "S", "<Enter>", "PrtScr", "F9") into
// the actual Windows Virtual-Key code plus a display string.
internal static class VirtualKeys
{
    public const ushort VK_SHIFT = 0x10;
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_MENU = 0x12; // Alt
    public const ushort VK_LWIN = 0x5B;

    private static readonly Dictionary<string, (ushort Vk, string Display)> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["enter"] = (0x0D, "Enter"),
            ["return"] = (0x0D, "Enter"),
            ["esc"] = (0x1B, "Esc"),
            ["escape"] = (0x1B, "Esc"),
            ["space"] = (0x20, "Space"),
            ["spacebar"] = (0x20, "Space"),
            ["tab"] = (0x09, "Tab"),
            ["backspace"] = (0x08, "Backspace"),
            ["delete"] = (0x2E, "Delete"),
            ["del"] = (0x2E, "Delete"),
            ["insert"] = (0x2D, "Insert"),
            ["ins"] = (0x2D, "Insert"),
            ["home"] = (0x24, "Home"),
            ["end"] = (0x23, "End"),
            ["pageup"] = (0x21, "Page Up"),
            ["pgup"] = (0x21, "Page Up"),
            ["pagedown"] = (0x22, "Page Down"),
            ["pgdn"] = (0x22, "Page Down"),
            ["up"] = (0x26, "Up"),
            ["down"] = (0x28, "Down"),
            ["left"] = (0x25, "Left"),
            ["right"] = (0x27, "Right"),
            ["prtscr"] = (0x2C, "PrtScr"),
            ["prtsc"] = (0x2C, "PrtScr"),
            ["printscreen"] = (0x2C, "PrtScr"),
        };

    private static readonly Dictionary<ushort, string> VkNames = new()
    {
        [0x08] = "Backspace",
        [0x09] = "Tab",
        [0x0D] = "Enter",
        [0x1B] = "Esc",
        [0x20] = "Space",
        [0x21] = "Page Up",
        [0x22] = "Page Down",
        [0x23] = "End",
        [0x24] = "Home",
        [0x25] = "Left",
        [0x26] = "Up",
        [0x27] = "Right",
        [0x28] = "Down",
        [0x2C] = "PrtScr",
        [0x2D] = "Insert",
        [0x2E] = "Delete",
        [0xBA] = ";",
        [0xBB] = "=",
        [0xBC] = ",",
        [0xBD] = "-",
        [0xBE] = ".",
        [0xBF] = "/",
        [0xC0] = "`",
        [0xDB] = "[",
        [0xDC] = "\\",
        [0xDD] = "]",
        [0xDE] = "'",
    };

    private static readonly Dictionary<char, ushort> CharToVk = new()
    {
        [';'] = 0xBA,
        ['='] = 0xBB,
        [','] = 0xBC,
        ['-'] = 0xBD,
        ['.'] = 0xBE,
        ['/'] = 0xBF,
        ['`'] = 0xC0,
        ['['] = 0xDB,
        ['\\'] = 0xDC,
        [']'] = 0xDD,
        ['\''] = 0xDE,
    };

    /// <summary>
    /// Resolves a raw manifest key token. Returns null if it can't be sent
    /// (e.g. an unmapped or sentinel code like 0).
    /// </summary>
    public static (ushort Vk, string Display)? Resolve(object? raw)
    {
        if (raw is int i)
            return ResolveVk((ushort)i);

        var s = raw?.ToString()?.Trim() ?? string.Empty;
        if (s.Length == 0)
            return null;

        if (int.TryParse(s, out var n))
            return ResolveVk((ushort)n);

        if (s.Length > 2 && s.StartsWith("<") && s.EndsWith(">"))
            s = s[1..^1];

        if (Aliases.TryGetValue(s, out var alias))
            return alias;

        var fMatch = Regex.Match(s, @"^[Ff](\d{1,2})$");
        if (fMatch.Success && int.TryParse(fMatch.Groups[1].Value, out var fn) && fn is >= 1 and <= 24)
            return ((ushort)(0x70 + fn - 1), $"F{fn}");

        if (s.Length == 1)
        {
            var c = s[0];
            var cu = char.ToUpperInvariant(c);
            if (cu is >= 'A' and <= 'Z')
                return ResolveVk(cu);
            if (cu is >= '0' and <= '9')
                return ResolveVk(cu);
            if (CharToVk.TryGetValue(char.ToLowerInvariant(c), out var vk))
                return ResolveVk(vk);
        }

        return null;
    }

    private static (ushort, string)? ResolveVk(ushort code)
    {
        if (code == 0)
            return null;
        if (code is >= 48 and <= 57)
            return (code, ((char)code).ToString());
        if (code is >= 65 and <= 90)
            return (code, ((char)code).ToString());
        if (code is >= 0x70 and <= 0x87)
            return (code, $"F{code - 0x70 + 1}");
        if (VkNames.TryGetValue(code, out var name))
            return (code, name);
        return null;
    }
}
