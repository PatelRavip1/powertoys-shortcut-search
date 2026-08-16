using System;

namespace shortcut_search;

internal enum ShortcutCategory
{
    Windows,
    PowerToys,
    App,
}

internal sealed class ShortcutEntry
{
    public string App { get; init; } = string.Empty;
    public string ActionName { get; init; } = string.Empty;
    public string WindowFilter { get; init; } = string.Empty;
    public ShortcutCategory Category { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Win { get; init; }
    public ushort[] Keys { get; init; } = [];
    public string Display { get; init; } = string.Empty;
}
