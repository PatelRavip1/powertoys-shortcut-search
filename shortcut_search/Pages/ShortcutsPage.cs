using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace shortcut_search;

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
        _items = BuildItems(string.Empty);
    }

    public override IListItem[] GetItems() => _items;

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _items = BuildItems(newSearch);
        RaiseItemsChanged();
    }

    private IListItem[] BuildItems(string raw)
    {
        raw = (raw ?? string.Empty).Trim();
        IEnumerable<ShortcutEntry> pool;

        if (raw.Length == 0)
        {
            // Default view: everything except per-app shortcuts.
            pool = _all
                .Where(s => s.Category is ShortcutCategory.Windows or ShortcutCategory.PowerToys)
                .OrderBy(s => s.App, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase);
        }
        else if (raw.StartsWith('/'))
        {
            // "/excel" -> all Excel shortcuts. "/excel copy" -> filtered further.
            var rest = raw[1..];
            var parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var appQuery = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            var searchQuery = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            var filtered = _all.Where(s => s.App.Contains(appQuery, StringComparison.OrdinalIgnoreCase));
            if (searchQuery.Length > 0)
            {
                filtered = filtered.Where(s => s.ActionName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            pool = Rank(filtered, searchQuery.Length > 0 ? searchQuery : appQuery).Take(50);
        }
        else
        {
            var filtered = _all
                .Where(s => s.Category is ShortcutCategory.Windows or ShortcutCategory.PowerToys)
                .Where(s => s.ActionName.Contains(raw, StringComparison.OrdinalIgnoreCase) || s.App.Contains(raw, StringComparison.OrdinalIgnoreCase));

            pool = Rank(filtered, raw).Take(50);
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
                if (string.Equals(s.ActionName, query, StringComparison.OrdinalIgnoreCase)) return 0;
                if (s.ActionName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
                if (s.App.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 2;
                return 3;
            })
            .ThenBy(s => s.App, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase);
    }
}
