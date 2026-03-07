# Native Menu Title Bar Integration Plan

Status: completed

## Goal

Use Avalonia native menus on platforms that export them, keep the current in-window menu as the fallback on unsupported platforms, and reclaim the title-bar space by moving high-frequency workspace commands into the title bar when native menu export is active.

## Current State

- The app uses an in-window `Menu` in the first chrome row.
- The second chrome row repeats some high-frequency commands and workspace controls.
- On macOS with integrated title bar, the menu row consumes space that should be available for title-bar actions.

## Target Behavior

1. Attach a real `NativeMenu` to `MainWindow`.
2. Let the platform export it when supported.
3. Keep the current in-window `Menu` visible only when native menu export is unavailable.
4. When native menu export is active and the integrated title bar is enabled:
   - move workspace preset and session actions into the title bar,
   - hide the duplicated preset/action strips from the second chrome row,
   - keep the remaining row focused on workspace summary and panel organization.

## Implementation Steps

1. Add a native menu factory in app UI services.
2. Build native top-level menus for:
   - File
   - Edit
   - View
   - Insert
   - Modify
   - Text
   - Commands
   - Control
   - Window
   - Help
3. Use commands directly where possible.
4. Use action delegates for file picker and publish/export flows already hosted in `MainWindow`.
5. Track `NativeMenu.IsNativeMenuExported` on the window and mirror that state into `MainWindowViewModel`.
6. Add viewmodel properties for:
   - native menu exported state,
   - fallback in-window menu visibility,
   - title-bar compact command strip visibility,
   - duplicate row strip visibility.
7. Update `MainWindow.axaml`:
   - keep the current menu as fallback,
   - add a compact title-bar action strip for native-export mode,
   - hide duplicated preset/action strips from row two when native-export mode is active.
8. Add tests for:
   - native menu layout state in the viewmodel,
   - native menu factory top-level structure.

## Validation

- Build the solution.
- Run focused tests for the native menu/viewmodel behavior.
- Smoke-launch the desktop app.

Completed with:

- native `NativeMenu` export on supported platforms
- in-window `Menu` fallback when native export is unavailable
- title-bar command relocation for native-export mode
- duplicate dock organizer menus removed from the second chrome row when title-bar mode is active
