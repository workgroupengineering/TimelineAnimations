# Dock Toolbar Consolidation Plan

## Problem Analysis

The docked workspace currently stacks too much shell chrome above the actual editing surfaces.

Current dock-mode top area:

1. App title + main menu row
2. Main-window workspace/action strip
3. Dock host preset/surface strip
4. Dock host file/session/status strip
5. Dock host tool/selection/surface metrics strip

That creates duplicate ownership for the same concepts:

| Concern | Main window row | Dock host rows | Result |
| --- | --- | --- | --- |
| Workspace presets | Yes | Yes | duplicate controls |
| Document/workspace summary | Yes | Yes | duplicate chips |
| Surface visibility/focus | Partial | Yes | split mental model |
| File/export/session actions | Yes | Yes | duplicate commands |
| Tool/selection context | Partial | Yes | too much top chrome |
| Playback/status metrics | Yes in panels | Yes | repeated data |

## UX Goals

- Reduce dock-mode top chrome from five rows to two rows total.
- Keep only one global command/workspace strip above the dock workspace.
- Move panel-specific context back into the panel headers that already exist:
  - Stage header
  - Frames header
  - Curves header
  - Dock tabs/panel titles
- Keep frequent actions one click away, but push lower-frequency actions into menus.
- Preserve all functionality:
  - workspace presets
  - dock/classic toggle
  - reveal/restore hidden surfaces
  - focus/show/hide/overlay surfaces
  - save/export/session commands
  - symbol edit/prototype/history commands

## Target Layout

### Before

```text
+---------------------------------------------------------------+
| App menu / document                                           |
+---------------------------------------------------------------+
| Workspace presets / dock toggle / focus / context / actions   |
+---------------------------------------------------------------+
| Dock presets / workspace summary / panel menus                |
+---------------------------------------------------------------+
| Open save export / symbol / history / status / help           |
+---------------------------------------------------------------+
| Tool summary / selection / surface tabs / metrics             |
+---------------------------------------------------------------+
| Actual dock workspace                                         |
```

### After

```text
+---------------------------------------------------------------+
| App menu / document                                           |
+---------------------------------------------------------------+
| Presets | Dock toggle | Reset | Context | Panels | More       |
|                                  Edit/Exit | Prototype | Save |
|                                  Undo | Redo | Reveal         |
+---------------------------------------------------------------+
| Tool rail | Docked stage / source / program / frames / curves |
|           | with panel-local headers and controls             |
```

## Command Placement

### Keep visible

- Workspace presets
- Dock toggle
- Reset dock
- Context summary
- Save
- Prototype
- Undo / Redo
- Edit Symbol or Exit Symbol
- Reveal surfaces when needed

### Move into menus

- Open
- Frame / sequence / project export
- Sample reset
- Keyboard/context help
- Surface show/focus/hide/overlay controls

## Implementation Steps

### Phase 1: Global dock strip

- Replace the dock-mode section of `MainWindow.axaml` row 1 with a single compact row.
- Keep the classic workspace row intact for non-dock mode.
- Add compact `Panels` and `More` menus for dock-only actions.

### Phase 2: Remove dock-host duplication

- Delete the three top rows from `DockWorkspaceHostView.axaml`.
- Start the dock host directly with the tool rail and dock workspace shell.
- Remove dock-host file/export click handlers that no longer have UI owners.

### Phase 3: Validation

- Update headless tests so they verify the dock host still renders and exposes the actual workspace surfaces.
- Rebuild and rerun tests.

## Success Criteria

- Dock mode shows only:
  - the main app menu row
  - one compact workspace/action row
- No functionality from the removed dock-host rows is lost.
- Stage, timeline, and inspector remain the primary visible surfaces.
- Headless tests continue to prove the dock workspace renders correctly.
