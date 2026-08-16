using System;
using System.Collections.Generic;
using System.Linq;

namespace shortcut_search;

internal static class ShortcutSearchEngine
{
    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["windows"] = "win",
        ["super"] = "win",
        ["meta"] = "win",
        ["cmd"] = "win",
        ["command"] = "win",
        ["control"] = "ctrl",
        ["ctl"] = "ctrl",
        ["alternate"] = "alt",
        ["opt"] = "alt",
        ["option"] = "alt",
        ["shift"] = "shift",
        ["shft"] = "shift",
        ["escape"] = "esc",
        ["esc"] = "esc",
        ["delete"] = "delete",
        ["del"] = "delete",
        ["insert"] = "insert",
        ["ins"] = "insert",
        ["return"] = "enter",
        ["enter"] = "enter",
        ["backspace"] = "backspace",
        ["bksp"] = "backspace",
        ["pageup"] = "page up",
        ["page up"] = "page up",
        ["pgup"] = "page up",
        ["pagedown"] = "page down",
        ["page down"] = "page down",
        ["pgdn"] = "page down",
        ["printscreen"] = "prtscr",
        ["print screen"] = "prtscr",
        ["prtsc"] = "prtscr",
        ["prtscr"] = "prtscr",
        ["spacebar"] = "space",
        ["space"] = "space",
    };

    private static readonly Dictionary<string, string[]> AppAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vscode"] = ["visual studio code", "vscode", "code"],
        ["code"] = ["visual studio code", "vscode", "code"],
        ["terminal"] = ["windows terminal", "terminal", "wt"],
        ["wt"] = ["windows terminal", "terminal", "wt"],
        ["explorer"] = ["file explorer", "windows explorer", "explorer"],
        ["chrome"] = ["google chrome", "chrome"],
        ["edge"] = ["microsoft edge", "edge"],
        ["excel"] = ["microsoft excel", "excel"],
        ["word"] = ["microsoft word", "word"],
        ["powerpoint"] = ["microsoft powerpoint", "powerpoint", "ppt"],
        ["ppt"] = ["microsoft powerpoint", "powerpoint", "ppt"],
        ["teams"] = ["microsoft teams", "teams"],
        ["outlook"] = ["microsoft outlook", "outlook"],
        ["pt"] = ["powertoys", "pt"],
        ["powertoys"] = ["powertoys", "power toys", "pt"],
        ["intellij"] = ["intellij idea", "intellij", "idea"],
        ["idea"] = ["intellij idea", "intellij", "idea"],
        ["notepad"] = ["notepad"],
        ["photoshop"] = ["adobe photoshop", "photoshop", "ps"],
        ["ps"] = ["adobe photoshop", "photoshop", "ps"],
        ["illustrator"] = ["adobe illustrator", "illustrator", "ai"],
        ["ai"] = ["adobe illustrator", "illustrator", "ai"],
        ["aftereffects"] = ["adobe after effects", "after effects", "ae"],
        ["ae"] = ["adobe after effects", "after effects", "ae"],
        ["indesign"] = ["adobe indesign", "indesign", "id"],
        ["id"] = ["adobe indesign", "indesign", "id"],
        ["gimp"] = ["gimp"],
        ["blender"] = ["blender"],
        ["figma"] = ["figma"],
        ["slack"] = ["slack"],
        ["discord"] = ["discord"],
        ["firefox"] = ["mozilla firefox", "firefox"],
        ["postman"] = ["postman"],
        ["telegram"] = ["telegram"],
    };

    public static string NormalizeKey(string key)
    {
        var k = key.Trim().Trim('<', '>');
        if (KeyAliases.TryGetValue(k, out var alias))
            return alias;
        return k.ToLowerInvariant();
    }

    public static IEnumerable<ShortcutEntry> Search(IReadOnlyList<ShortcutEntry> allShortcuts, string? rawQuery, int maxResults = 75)
    {
        var q = (rawQuery ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            // Default view: Windows & PowerToys shortcuts first, then sorted alphabetically by App & Action
            return allShortcuts
                .OrderBy(s => s.Category switch
                {
                    ShortcutCategory.Windows => 0,
                    ShortcutCategory.PowerToys => 1,
                    _ => 2,
                })
                .ThenBy(s => s.App, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults);
        }

        var qLower = q.ToLowerInvariant();

        // Check for slash / app prefix: e.g. "/excel copy" or "/vscode" or "@chrome"
        if (qLower.StartsWith('/') || qLower.StartsWith('@'))
        {
            var rest = qLower[1..].Trim();
            var parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var appQuery = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            var subQuery = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            var filteredByApp = allShortcuts.Where(s =>
                s.App.Contains(appQuery, StringComparison.OrdinalIgnoreCase) ||
                (AppAliases.TryGetValue(appQuery, out var aliases) && aliases.Any(a => s.App.Contains(a, StringComparison.OrdinalIgnoreCase))));

            if (subQuery.Length == 0)
            {
                return filteredByApp
                    .OrderBy(s => s.ActionName, StringComparer.OrdinalIgnoreCase)
                    .Take(maxResults);
            }

            return filteredByApp
                .Select(s => (Score: ScoreItem(s, subQuery), Entry: s))
                .Where(x => x.Score.HasValue)
                .OrderBy(x => x.Score!.Value)
                .ThenBy(x => x.Entry.ActionName, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Entry)
                .Take(maxResults);
        }

        return allShortcuts
            .Select(s => (Score: ScoreItem(s, qLower), Entry: s))
            .Where(x => x.Score.HasValue)
            .OrderBy(x => x.Score!.Value)
            .ThenBy(x => x.Entry.Category switch
            {
                ShortcutCategory.Windows => 0,
                ShortcutCategory.PowerToys => 1,
                _ => 2,
            })
            .ThenBy(x => x.Entry.App, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Entry.ActionName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry)
            .Take(maxResults);
    }

    private static int? ScoreItem(ShortcutEntry s, string qLower)
    {
        var appLower = s.App.ToLowerInvariant();
        var actionLower = s.ActionName.ToLowerInvariant();
        var descLower = s.Description.ToLowerInvariant();
        var secLower = s.Section.ToLowerInvariant();
        var catLower = s.Category switch
        {
            ShortcutCategory.Windows => "windows",
            ShortcutCategory.PowerToys => "powertoys",
            _ => "app",
        };

        var qClean = qLower.Replace('+', ' ').Replace('-', ' ').Replace('_', ' ').Trim();
        var words = qClean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return null;

        var normWords = words.Select(NormalizeKey).ToArray();

        // 1. Direct whole-query match checks
        if (actionLower == qLower || actionLower == qClean)
            return 0; // Exact action name match

        if (normWords.Length == s.NormalizedKeys.Length && normWords.All(nw => s.NormalizedKeys.Contains(nw, StringComparer.OrdinalIgnoreCase)))
            return 1; // Exact key combination match (e.g. "ctrl c" / "ctrl+c" / "win e" / "alt f4")

        if (actionLower.StartsWith(qLower, StringComparison.OrdinalIgnoreCase) || actionLower.StartsWith(qClean, StringComparison.OrdinalIgnoreCase))
            return 5; // Action starts with query

        if (appLower == qLower || appLower == qClean)
            return 10; // Exact app name match

        // Key combination subset match (e.g. "ctrl shift")
        var isComboSubset = normWords.Length > 1 && normWords.All(nw => s.NormalizedKeys.Contains(nw, StringComparer.OrdinalIgnoreCase));
        if (isComboSubset)
            return 8;

        // 2. Tokenized multi-word search
        var appWords = appLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var actionWords = actionLower.Split([' ', '(', ')', '[', ']', ',', '.', '-', '_', '→', '/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var secWords = secLower.Split([' ', '(', ')', '[', ']', ',', '.', '-', '_', '→', '/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        var hasAppMatch = false;
        var hasActionMatch = false;
        var hasKeyMatch = false;
        var hasSecMatch = false;

        foreach (var (w, nw) in words.Zip(normWords))
        {
            var isShort = w.Length <= 1;

            // Key match
            var matchedKey = s.NormalizedKeys.Contains(nw, StringComparer.OrdinalIgnoreCase) ||
                             s.NormalizedKeys.Contains(w, StringComparer.OrdinalIgnoreCase);

            // App match
            var matchedApp = false;
            if (nw == "win" || w == "win" || w == "windows")
            {
                matchedApp = appLower.Contains("windows", StringComparison.OrdinalIgnoreCase) || catLower == "windows";
            }
            else if (isShort)
            {
                matchedApp = appWords.Any(aw => aw.Equals(w, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                matchedApp = appLower.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                             appLower.Contains(nw, StringComparison.OrdinalIgnoreCase) ||
                             (AppAliases.TryGetValue(w, out var aliases) && aliases.Any(a => appLower.Contains(a, StringComparison.OrdinalIgnoreCase))) ||
                             (AppAliases.TryGetValue(nw, out var nAliases) && nAliases.Any(a => appLower.Contains(a, StringComparison.OrdinalIgnoreCase)));
            }

            // Action match
            var matchedAction = false;
            if (nw == "win" || w == "win")
            {
                matchedAction = actionWords.Any(aw => aw.Equals("win", StringComparison.OrdinalIgnoreCase) || aw.Equals("windows", StringComparison.OrdinalIgnoreCase));
            }
            else if (isShort)
            {
                matchedAction = actionWords.Any(aw => aw.Equals(w, StringComparison.OrdinalIgnoreCase) || aw.Equals(nw, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                matchedAction = actionLower.Contains(w, StringComparison.OrdinalIgnoreCase) || actionLower.Contains(nw, StringComparison.OrdinalIgnoreCase);
            }

            // Section match
            var matchedSec = false;
            if (nw == "win" || w == "win")
            {
                matchedSec = secWords.Any(sw => sw.Equals("win", StringComparison.OrdinalIgnoreCase) || sw.Equals("windows", StringComparison.OrdinalIgnoreCase));
            }
            else if (isShort)
            {
                matchedSec = secWords.Any(sw => sw.Equals(w, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                matchedSec = secLower.Contains(w, StringComparison.OrdinalIgnoreCase) || secLower.Contains(nw, StringComparison.OrdinalIgnoreCase);
            }

            // Description match
            var matchedDesc = !string.IsNullOrEmpty(descLower) && !isShort && (descLower.Contains(w, StringComparison.OrdinalIgnoreCase) || descLower.Contains(nw, StringComparison.OrdinalIgnoreCase));

            // Category match
            var matchedCat = !isShort && (catLower.Contains(w, StringComparison.OrdinalIgnoreCase) || catLower.Contains(nw, StringComparison.OrdinalIgnoreCase));

            if (!matchedKey && !matchedApp && !matchedAction && !matchedSec && !matchedDesc && !matchedCat)
            {
                return null; // Every token must match somewhere
            }

            if (matchedApp) hasAppMatch = true;
            if (matchedAction || matchedDesc) hasActionMatch = true;
            if (matchedKey) hasKeyMatch = true;
            if (matchedSec) hasSecMatch = true;
        }

        int score;
        if (hasAppMatch && hasKeyMatch)
            score = 14;
        else if (hasAppMatch && hasActionMatch)
            score = 15;
        else if (hasSecMatch && hasActionMatch)
            score = 22;
        else if (hasActionMatch && qClean.Length > 0 && actionLower.Contains(qClean, StringComparison.OrdinalIgnoreCase))
            score = 16;
        else if (hasKeyMatch && hasActionMatch)
            score = 24;
        else if (hasActionMatch)
            score = 20;
        else if (hasAppMatch)
            score = 30;
        else if (hasSecMatch)
            score = 35;
        else if (isComboSubset)
            score = 40;
        else
            score = 45;

        // Boost global system shortcuts when action matches
        if (s.Category is ShortcutCategory.Windows or ShortcutCategory.PowerToys && hasActionMatch)
        {
            score -= 1;
        }

        return score;
    }
}
