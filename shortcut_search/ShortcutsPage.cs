using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace shortcut_search;

// NOTE: Rename this class (and this file) to match whatever your generated
// project already calls its page, e.g. `ShortcutLauncherPage`, so it slots
// into the template's existing top-level command wiring without you having
// to touch <ExtensionName>CommandsProvider.cs at all.
internal sealed partial class ShortcutsPage : DynamicListPage
{
    private readonly List<ShortcutEntry> _all;
    private IListItem[] _items;

    public ShortcutsPage()
    {
        Icon = new IconInfo("\uE765"); // keyboard glyph
        Title = "Shortcuts";
        Name = "Search";

        _all = ShortcutManifestLoader.LoadAll();
        _items = BuildItems("");
    }

    public override IListItem[] GetItems() => _items;

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _items = BuildItems(newSearch);
        RaiseItemsChanged();
    }

    private IListItem[] BuildItems(string raw)
    {
        raw = (raw ?? "").Trim();
        IEnumerable<ShortcutEntry> pool;

        if (raw.Length == 0)
        {
            // Default view: everything except per-app shortcuts.
            pool = _all
                .Where(s => s.Category is ShortcutCategory.Windows or ShortcutCategory.PowerToys)
                .OrderBy(s => s.App, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase);
        }
        else if (raw.StartsWith("/"))
        {
            // "/excel" -> all Excel shortcuts. "/excel copy" -> filtered further.
            var rest = raw[1..];
            var parts = rest.Split(' ', 2);
            var appQuery = parts[0].ToLowerInvariant();
            var searchQuery = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "";

            var filtered = _all.Where(s => s.App.ToLowerInvariant().Contains(appQuery));
            if (searchQuery.Length > 0)
                filtered = filtered.Where(s => s.ActionName.ToLowerInvariant().Contains(searchQuery));

            pool = Rank(filtered, searchQuery.Length > 0 ? searchQuery : appQuery).Take(50);
        }
        else
        {
            var query = raw.ToLowerInvariant();
            var filtered = _all
                .Where(s => s.Category is ShortcutCategory.Windows or ShortcutCategory.PowerToys)
                .Where(s => s.ActionName.ToLowerInvariant().Contains(query) || s.App.ToLowerInvariant().Contains(query));

            pool = Rank(filtered, query).Take(50);
        }

        return pool
            .Select(s => (IListItem)new ListItem(new RunShortcutCommand(s))
            {
                Title = $"{s.App} \u2192 {s.ActionName}",
                Subtitle = s.Display,
            })
            .ToArray();
    }

    private static IEnumerable<ShortcutEntry> Rank(IEnumerable<ShortcutEntry> items, string query)
    {
        return items
            .OrderBy(s =>
            {
                var name = s.ActionName.ToLowerInvariant();
                var app = s.App.ToLowerInvariant();
                if (name == query) return 0;
                if (name.StartsWith(query)) return 1;
                if (app.StartsWith(query)) return 2;
                return 3;
            })
            .ThenBy(s => s.App, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase);
    }
}
