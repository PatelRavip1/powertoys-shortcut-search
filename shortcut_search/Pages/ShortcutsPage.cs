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
        PlaceholderText = "Search by action (copy, format), app (vscode, chrome), keys (ctrl+c, win+e), or type...";

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
        var matched = ShortcutSearchEngine.Search(_all, raw, 75);

        return matched
            .Select(s =>
            {
                var categoryTag = s.Category switch
                {
                    ShortcutCategory.Windows => "Windows",
                    ShortcutCategory.PowerToys => "PowerToys",
                    _ => s.App,
                };

                var scopeDescription = s.Category == ShortcutCategory.App
                    ? (string.IsNullOrWhiteSpace(s.WindowFilter) ? "App window" : $"Target: `{s.WindowFilter}`")
                    : "Global (system-wide shortcut)";

                var detailsBody = $"### {s.App}\n\n" +
                    $"**Shortcut:** `{s.Display}`\n\n" +
                    (string.IsNullOrWhiteSpace(s.Section) ? string.Empty : $"**Section:** {s.Section}\n\n") +
                    (string.IsNullOrWhiteSpace(s.Description) ? string.Empty : $"**Description:** {s.Description}\n\n") +
                    $"**Scope:** {scopeDescription}";

                return (IListItem)new ListItem(new RunShortcutCommand(s))
                {
                    Title = $"{s.App} \u2192 {s.ActionName}",
                    Subtitle = string.IsNullOrEmpty(s.Section) ? s.Display : $"{s.Display}  \u2022  {s.Section}",
                    Tags = [new Tag(categoryTag)],
                    Details = new Details
                    {
                        Title = s.ActionName,
                        Body = detailsBody,
                    },
                    MoreCommands = [
                        new CommandContextItem(new CopyTextCommand(s.Display))
                        {
                            Title = $"Copy Shortcut ({s.Display})",
                        },
                        new CommandContextItem(new CopyTextCommand(s.ActionName))
                        {
                            Title = "Copy Action Name",
                        },
                    ],
                };
            })
            .ToArray();
    }
}
