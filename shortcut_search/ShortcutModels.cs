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
    public string App { get; init; } = "";
    public string ActionName { get; init; } = "";
    public string WindowFilter { get; init; } = "";
    public ShortcutCategory Category { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Win { get; init; }
    public ushort[] Keys { get; init; } = Array.Empty<ushort>();
    public string Display { get; init; } = "";
}
