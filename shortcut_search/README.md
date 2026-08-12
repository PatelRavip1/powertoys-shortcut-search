# Shortcut Launcher — Command Palette extension

Same idea as the Python popup, rebuilt as a proper PowerToys Command Palette
extension: type in Command Palette, get a live-filtered list of shortcuts,
press Enter to actually run one.

## What's different from the Python version

- No separate hotkey/popup — it lives inside Command Palette itself (`Win`
  by default, or whatever you've bound it to).
- Sending keys uses raw Windows `SendInput` with Virtual-Key codes directly
  (no string key names needed — PowerToys' own manifest is already VK codes).
- Default view = Windows + PowerToys shortcuts. Type `/excel` to browse an
  app's shortcuts, `/excel copy` to filter within it. Same behavior as before.

## Setup

1. Install PowerToys, enable Command Palette, enable Developer Mode
   (Settings → Privacy & security → For developers), and install Visual
   Studio with the WinUI / Windows App SDK workload.
2. In Command Palette, run **"Create a new extension"**. Give it a name,
   e.g. `ShortcutLauncher`. This generates the real project — manifest, COM
   server registration, `.csproj` — none of which I can safely hand-write
   outside Visual Studio's tooling.
3. Add the YAML parser package to the generated project:

   ```
   dotnet add package YamlDotNet
   ```

4. Copy these files into the generated project folder (next to the
   `.csproj`, or in a subfolder — either works):
   - `VirtualKeys.cs`
   - `ShortcutModels.cs`
   - `ShortcutManifestLoader.cs`
   - `NativeMethods.cs`
   - `KeySender.cs`
   - `WindowFocus.cs`
   - `RunShortcutCommand.cs`

5. **Namespace:** every file uses `namespace shortcut_search;` — change
   this to match whatever namespace your generated project actually uses
   (it'll be your `<ExtensionName>`).

6. **Wire up the page:** don't add `ShortcutsPage.cs` as a new file — instead,
   open your generated `Pages\<ExtensionName>Page.cs`, delete its contents,
   and replace them with `ShortcutsPage.cs`'s contents, renaming the class
   back to `<ExtensionName>Page`. The template already wires that class up
   as your top-level command, so this avoids touching the CommandsProvider
   file at all.

7. In Visual Studio: **Build → Deploy `<ExtensionName>`** (not just Build —
   deploying is what registers the package). Then in Command Palette, run
   **Reload** (the one subtitled "Reload Command Palette Extension").

## Honest caveats

- I can't compile or run this here — no Windows, no Visual Studio, no
  Command Palette. It's written directly against the documented API shapes
  (`DynamicListPage`, `UpdateSearchText`, `ListItem`, `InvokableCommand`,
  `CommandResult.Hide()`), but the extension SDK is new and still evolving,
  so a property or method name may not match exactly what IntelliSense
  shows you. If something doesn't compile, it's almost certainly a small
  naming mismatch — check the matching page under
  `Microsoft.CommandPalette.Extensions.Toolkit` in the
  [API reference](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/sdk-namespaces).
- `RunShortcutCommand` focuses the target app by matching `WindowFilter`
  against a running process name. If the app isn't open, nothing happens —
  same limitation as the Python version.
- First run: if the list comes up empty, your PowerToys install path
  differs from what's hardcoded in `ShortcutManifestLoader.ManifestDirs` —
  add your actual path there.
