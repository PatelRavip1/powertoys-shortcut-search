# powertoys-shortcut-search

A Microsoft PowerToys Command Palette extension that provides live-filtered search across Windows, PowerToys, and application keyboard shortcuts with one-press execution.

## Features

- **Integrated Experience:** Runs directly inside PowerToys Command Palette as an out-of-process COM extension.
- **Fast Search & Ranking:** Instant prefix and fuzzy matching on shortcut names, apps, and key combinations.
- **App-Specific Filtering:** Type `/` (e.g. `/excel` or `/excel copy`) to browse shortcuts for specific applications.
- **Direct Key Injection:** Uses Windows Input Injection broker APIs with fallback to dedicated APIs (e.g. `LockWorkStation` for `Win+L`).
- **Window Focus Handling:** Automatically restores and brings target application windows to the foreground before injecting keystrokes.
- **Extended Manifest Discovery:** Automatically searches multiple standard PowerToys installation locations and custom WinGet shortcut manifests.

## Architecture & Structure

| Path | Purpose |
|---|---|
| `shortcut_search/Pages/ShortcutsPage.cs` | Reactive `DynamicListPage` implementing search and ranking |
| `shortcut_search/RunShortcutCommand.cs` | `InvokableCommand` executing shortcuts and triggering window focus |
| `shortcut_search/KeySender.cs` | WinRT `InputInjector` keystroke simulator with extended-key support |
| `shortcut_search/WindowFocus.cs` | Process-to-window enumeration and foreground restoration |
| `shortcut_search/ShortcutManifestLoader.cs` | YAML deserializer for PowerToys Shortcut Guide manifests |
| `shortcut_search/VirtualKeys.cs` | Token-to-VK mapper with culture-invariant hex/numeric/alias support |
| `shortcut_search/ShortcutModels.cs` | Data models and enums |
| `shortcut_search/shortcut_searchCommandsProvider.cs` | Extension entry point registering top-level commands |
| `shortcut_search/shortcut_search.cs` | Root `IExtension` COM implementation |
| `shortcut_search/Program.cs` | COM server host lifecycle |
| `shortcut_search/Package.appxmanifest` | MSIX package manifest with Command Palette extension activation |

## Build & Deploy

1. **Build solution:**
   ```powershell
   dotnet build shortcut_search.sln -p:Platform=x64
   ```
2. **Deploy MSIX package:**
   In Visual Studio, right click `shortcut_search` → **Deploy** (or press F5).
3. **Reload in Command Palette:**
   Open Command Palette (`Win` or configured hotkey), search for **Reload**, and select **Reload Command Palette extensions**.

## Requirements

- Windows 10 version 2004 (build 19041) or Windows 11
- PowerToys with Command Palette enabled
- .NET 10 SDK with Windows App SDK workload
