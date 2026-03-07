# Dock Workspace Classic Parity Plan

## Goal

Make the docked workspace the single fully featured authoring surface by surfacing all features that still only exist in the legacy/classic shell.

## Current Gap Analysis

### Docked `Project` tool currently misses

- Classic `Assets` tab
- Palette item staging surface
- Avalonia toolbox surface
- Full library metadata editing
- Symbol capture/update controls
- Component capture/place workflow
- Media import buttons and richer media details

### Docked `Inspector` tool currently misses

- Most advanced object properties from classic `Properties`
- Symbol instance metadata
- Advanced transform editors (`scale`, `skew`, rotation)
- Alignment commands
- Hierarchy/power editing controls
- Guide binding and authoring metadata
- Full style editors
- Full text-field/XFL metadata
- Avalonia control property editors
- Compositing and Flash effects
- Media/audio finishing and editorial tools
- Full `Motion + Actions` surface:
  - scene markers
  - transitions
  - behaviors (`on click`, triggers, actions)
  - frame action scripts
  - behavior scripts
  - visual states

### Docked `Output` tool currently misses

- Full interop workspace with code + visual split preview
- Preview diagnostics and last-run diagnostics
- Full publish profile editor
- Publish validation surface
- Classic import/export/preview buttons in-panel

## Implementation Strategy

### 1. Reuse the classic feature groups instead of redesigning them

- `DockProjectToolView` will host the classic left-side tabs:
  - `Project`
  - `Assets`
- `DockInspectorToolView` will host the classic right-side tabs:
  - `Properties`
  - `Motion + Actions`
- `DockOutputToolView` will host the classic output workspace as a full scrollable surface

This keeps feature behavior identical while making Dock the primary workspace.

### 2. Bind dock views directly to `Host`

- Keep the dock tool view models as thin wrappers
- Set the inner content `DataContext` to `Host`
- Reuse the existing `MainWindowViewModel` command/property surface
- Avoid duplicate feature state in dock-specific view models

### 3. Reintroduce file-dialog-backed actions inside dock tools

- Add a shared dock file action helper for:
  - animation import/export
  - media import
  - publish preview/export
- Reuse the same services already used by the main window

### 4. Strengthen validation

- Add headless tests for:
  - dock `Project` tool classic tabs and palette/toolbox rendering
  - dock `Inspector` classic tabs and behaviors/states surface rendering
  - dock `Output` code/visual preview and publish surface rendering
- Keep the existing full dock-host render test

## Execution Steps

1. Replace simplified `DockProjectToolView` with classic `Project` and `Assets` tabs.
2. Replace simplified `DockInspectorToolView` with classic `Properties` and `Motion + Actions` tabs.
3. Replace simplified `DockOutputToolView` with the full classic output workspace.
4. Add shared dock file action helpers and wire import/export/publish buttons.
5. Add focused headless tests for the three upgraded dock tools.
6. Run build, targeted headless tests, full tests, and a live runtime smoke screenshot.

## Success Criteria

- No feature remains available only in the classic workspace for:
  - assets/toolbox/library/components/media
  - object/scene/style/compositing/media/frame editing
  - behaviors/actions/visual states
  - interop preview/import/export
  - publish profile editing/validation
- The docked workspace renders all major authoring surfaces without blank or simplified substitutes.
