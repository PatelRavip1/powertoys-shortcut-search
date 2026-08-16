using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace shortcut_search;

// Raw shapes matching PowerToys' Shortcut Guide manifest schema.
internal sealed class ManifestRoot
{
    public string? PackageName { get; set; }
    public string? Name { get; set; }
    public string? WindowFilter { get; set; }
    public bool? BackgroundProcess { get; set; }
    public List<ManifestSection>? Shortcuts { get; set; }
}

internal sealed class ManifestSection
{
    public string? SectionName { get; set; }
    public List<ManifestProperty>? Properties { get; set; }
}

internal sealed class ManifestProperty
{
    public string? Name { get; set; }
    public List<ManifestCombo>? Shortcut { get; set; }
}

internal sealed class ManifestCombo
{
    public bool? Ctrl { get; set; }
    public bool? Alt { get; set; }
    public bool? Shift { get; set; }
    public bool? Win { get; set; }

    // Declared as strings: PowerToys' own manifest uses raw VK-code numbers
    // here, other manifests use names like "S" or "<Enter>". Either
    // deserializes fine into a string.
    public List<string>? Keys { get; set; }
}

internal static class ShortcutManifestLoader
{
    private static readonly string[] ManifestDirs =
    {
        Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\PowerToys\Assets\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\PowerToys\modules\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\PowerToys\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\PowerToys\Assets\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\PowerToys\modules\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\PowerToys\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\PowerToys\Assets\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\PowerToys\ShortcutGuide\Manifests"),
        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Microsoft\WinGet\KeyboardShortcuts"),
    };

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming.")]
    public static List<ShortcutEntry> LoadAll()
    {
        var results = new List<ShortcutEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        foreach (var dir in ManifestDirs)
        {
            if (!Directory.Exists(dir))
                continue;

            var files = Directory.EnumerateFiles(dir, "*.yml")
                .Concat(Directory.EnumerateFiles(dir, "*.yaml"));

            foreach (var path in files)
            {
                ManifestRoot? data;
                try
                {
                    using var reader = new StreamReader(path);
                    data = deserializer.Deserialize<ManifestRoot>(reader);
                }
                catch
                {
                    continue;
                }

                if (data?.Shortcuts is null)
                    continue;

                var packageName = data.PackageName ?? "";
                string appName;
                if (!string.IsNullOrWhiteSpace(data.Name))
                    appName = data.Name!;
                else if (packageName.Contains("WindowsNT", StringComparison.OrdinalIgnoreCase))
                    appName = "Windows";
                else
                    appName = Path.GetFileNameWithoutExtension(path);

                var windowFilter = data.WindowFilter ?? "";

                ShortcutCategory category;
                if (packageName.Contains("WindowsNT", StringComparison.OrdinalIgnoreCase))
                    category = ShortcutCategory.Windows;
                else if (string.Equals(appName, "PowerToys", StringComparison.OrdinalIgnoreCase))
                    category = ShortcutCategory.PowerToys;
                else
                    category = ShortcutCategory.App;

                foreach (var section in data.Shortcuts)
                {
                    foreach (var prop in section.Properties ?? [])
                    {
                        var actionName = prop.Name ?? string.Empty;

                        foreach (var combo in prop.Shortcut ?? [])
                        {
                            var keyDisplays = new List<string>();
                            var vkCodes = new List<ushort>();
                            var ok = true;

                            foreach (var rawKey in combo.Keys ?? [])
                            {
                                var resolved = VirtualKeys.Resolve(rawKey);
                                if (resolved is null)
                                {
                                    ok = false;
                                    break;
                                }
                                vkCodes.Add(resolved.Value.Vk);
                                keyDisplays.Add(resolved.Value.Display);
                            }

                            if (!ok || vkCodes.Count == 0)
                                continue;

                            var modParts = new List<string>();
                            if (combo.Ctrl == true) modParts.Add("Ctrl");
                            if (combo.Alt == true) modParts.Add("Alt");
                            if (combo.Shift == true) modParts.Add("Shift");
                            if (combo.Win == true) modParts.Add("Win");

                            var display = string.Join("+", modParts.Concat(keyDisplays));
                            var dedupeKey = $"{appName}|{actionName}|{display}";
                            if (!seen.Add(dedupeKey))
                                continue;

                            results.Add(new ShortcutEntry
                            {
                                App = appName,
                                ActionName = actionName,
                                WindowFilter = windowFilter,
                                Category = category,
                                Ctrl = combo.Ctrl == true,
                                Alt = combo.Alt == true,
                                Shift = combo.Shift == true,
                                Win = combo.Win == true,
                                Keys = vkCodes.ToArray(),
                                Display = display,
                            });
                        }
                    }
                }
            }
        }

        return results;
    }
}
