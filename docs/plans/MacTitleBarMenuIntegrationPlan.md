# macOS Title Bar Menu Integration Plan

## Goal

Use Avalonia's extended client area support so the app menu and primary document chrome live inside the native macOS title bar region, while leaving Windows and Linux behavior unchanged.

## Local Avalonia Analysis

The installed Avalonia runtime exposes the needed window APIs:

- `Window.ExtendClientAreaToDecorationsHint`
- `Window.ExtendClientAreaChromeHints`
- `Window.ExtendClientAreaTitleBarHeightHint`
- `ExtendClientAreaChromeHints.PreferSystemChrome`
- `ExtendClientAreaChromeHints.OSXThickTitleBar`

Those are documented in the local package metadata under:

- `/Users/wieslawsoltes/.nuget/packages/avalonia/11.3.11/lib/net8.0/Avalonia.Controls.xml`

## Layout Strategy

### Non-macOS

- Keep the current top chrome layout.
- Do not extend client area into decorations.

### macOS

- Extend the client area into the title bar.
- Use `PreferSystemChrome | OSXThickTitleBar` for proper native toolbar placement.
- Keep system traffic lights.
- Add a fixed leading inset so the app title and menu do not overlap the traffic lights.
- Tighten the outer top margin so the first chrome row visually becomes the title bar.

## Implementation Steps

1. Add platform-aware title bar properties to `MainWindowViewModel`.
2. Bind `MainWindow` extended-client-area properties to those viewmodel properties.
3. Update the main root margin for macOS integrated title bar mode.
4. Add a leading spacer column in the top chrome row to reserve traffic-light space.
5. Slightly increase top-row padding for the integrated title bar.
6. Rebuild and smoke-test.

## Success Criteria

- On macOS, the first menu/document row sits in the title bar region.
- The traffic lights no longer overlap the title/menu area.
- On other platforms, the window keeps the current chrome layout.
