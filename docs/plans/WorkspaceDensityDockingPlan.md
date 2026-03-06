# Workspace Density And Docking Plan

## Goal

Make the editor shell substantially more space-efficient while adding practical panel management:

- less vertical chrome and dead space
- more usable room for stage and timelines
- docked, overlay, and hidden panel modes
- layout presets for different tasks

This pass is aligned with the broader studio-workflow roadmap, but it is scoped to the current shell so it can land as a coherent end-to-end improvement instead of a full framework migration.

## UX Problems To Fix

1. Repeated headers, large stack spacing, and thick panel padding waste vertical room.
2. The stage and timelines lose space to explanatory rows and oversized toolbar strips.
3. Side docks are always present even when the user wants a stage-first or timeline-first workflow.
4. The shell has splitters, but not true panel modes or layout presets.

## Design Targets

### Density

- shrink top chrome height
- reduce panel padding and intra-panel spacing
- remove nonessential descriptive rows
- compress timeline control bars so more height goes to frame and curve surfaces

### Panel Modes

Each major workspace panel should support:

- `Docked`
- `Overlay`
- `Hidden`

Initial panels in scope:

- left dock
- right inspector
- bottom timeline

### Layout Presets

Ship three deterministic presets:

1. `Classic`
2. `StageFocus`
3. `TimelineFocus`

`Custom` is derived automatically when users manually change panel modes.

## Implementation Plan

### Step 1. Introduce Workspace Layout State

- add `WorkspacePanelMode`
- add `WorkspaceLayoutPreset`
- store panel modes and docked sizes in `MainWindowViewModel`
- expose computed bindable layout properties for docked widths, overlay sizes, visibility, spans, alignment, and z-order

### Step 2. Rework Shell Density

- replace descriptive text in the workspace strip with functional workspace controls
- reduce top-row height and compact control metrics further
- shrink the tools stack height and reduce padding/spacing in the stage and timeline shells
- remove or compress vertical bands that do not directly support editing

### Step 3. Implement Docked And Overlay Hosts

- bind left and right dock hosts to docked or overlay behavior without losing splitter-based resizing
- bind the bottom timeline host to docked or overlay behavior
- hide splitters automatically when a panel is not docked

### Step 4. Add Workspace Controls

- add preset selection in the top workspace strip
- add per-panel mode selectors for left, right, and timeline panels
- keep the interaction deterministic: no drag docking in this pass

### Step 5. Validate

- verify the shell still loads with compiled bindings
- verify overlay mode does not break stage interaction
- verify docked mode still resizes through splitters
- run build, tests, and smoke launch

### Step 6. Persist And Restore Workspace State

- persist panel modes and docked sizes to a local workspace-layout file
- restore the last workspace automatically on startup
- add direct restore and show or hide panel actions in the workspace strip
- preserve a sensible restore mode when a panel is collapsed from hidden state

### Step 7. Add Focus Actions And Collapsed Edge Handles

- add direct focus actions for stage-first, timeline-first, tools-first, and inspector-first work
- surface collapsed panel restore handles on the workspace edges instead of relying only on top-strip controls
- keep focus actions deterministic and backed by persisted workspace state
- avoid adding tall new rows; use compact chrome only

### Step 8. Add Local Panel Chrome Controls

- add compact dock, overlay, and hide controls in the left dock, timeline, and inspector headers
- make the active panel mode visible without forcing the user to inspect the top workspace strip
- keep the controls local to the panel so workspace reshaping feels immediate
- preserve the compact layout and avoid adding another full toolbar band

### Step 9. Add Workspace Keyboard Shortcuts

- add window-level shortcuts for the main workspace focus targets
- add shortcuts for toggling the left dock, right inspector, and timeline panel
- expose a compact shortcut hint surface without adding another visible toolbar row
- route shortcuts through the same deterministic viewmodel commands as the visible UI

### Step 10. Add Workspace Context Menus

- add right-click workspace menus on the workspace strip and panel headers
- expose focus, restore, dock, overlay, and hide actions without adding more permanent chrome
- keep all menu actions routed through the same viewmodel commands as buttons and shortcuts
- make local panel menu choices match the panel they belong to

## Acceptance

- the shell is visibly denser than the current screenshot
- the stage and timeline surfaces gain space from compaction
- left, right, and timeline panels can be docked, overlayed, or hidden
- users can switch between `Classic`, `StageFocus`, and `TimelineFocus`
- workspace changes persist and can be restored quickly
- hidden docks can be restored from local edge handles and key focus actions
- visible docks expose local dock, overlay, and hide controls in their own chrome
- the main workspace operations are reachable from keyboard shortcuts
- workspace operations are also reachable from contextual menus on the strip and dock headers
- the app remains buildable and runnable
