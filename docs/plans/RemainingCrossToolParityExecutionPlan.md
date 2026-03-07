# Remaining Cross-Tool Parity Execution Plan

## Goal

Implement the remaining phases from `CrossToolParityRoadmap.md`:

- Phase 19: hierarchy and power editing
- Phase 20: editorial finishing
- Phase 21: Dock-for-Avalonia workspace completion

The implementation keeps the existing editor functional while moving the missing
capabilities into explicit core services, ViewModel orchestration, and a Dock-backed
workspace layer.

## Gap Summary

### Hierarchy and power editing

Missing today:

- layer folders and nested layer organization
- collapse / expand hierarchy presentation
- solo / mute semantics on layers
- edit-multiple-frames workflow
- richer layer and frame context actions

Implementation:

1. Extend `TimelineLayer` with hierarchy and track-state metadata.
2. Add a `LayerHierarchyService` for normalization, flattening, and folder parenting.
3. Add a `PowerEditingService` for edit-multiple-frames operations across frame ranges.
4. Update layer/timeline row view models to expose hierarchy depth, folder state, and solo/mute.
5. Extend the shell with hierarchy affordances and power-editing commands.

Acceptance:

- folders can contain child layers
- hierarchy order is stable and persisted
- folders can collapse and expand without losing data
- solo / mute affects preview and export sampling
- selection edits can be applied across a selected frame range

### Editorial finishing

Missing today:

- razor / split editing
- slip / slide / ripple editing
- broader transition options
- audio mix controls and visual metering

Implementation:

1. Add an `EditorialEditingService` for split, slip, slide, and ripple operations.
2. Extend media settings with gain, pan, fade in, and fade out.
3. Add an `AudioMixService` for effective clip gain/pan/fade evaluation and UI meters.
4. Expose finishing controls in the timeline and inspector.
5. Extend export/runtime preview paths to honor mute/solo and audio finishing metadata.

Acceptance:

- a selected media clip can be split at the playhead
- clips can be slipped, slid, and rippled with deterministic bounds handling
- track mute / solo affects playback and output
- audio layers expose gain, pan, and fade controls with readable meter feedback

### Dock-for-Avalonia workspace completion

Missing today:

- actual Dock-backed panel model
- persisted dock layout independent from the handcrafted shell grid
- dockable monitor/timeline/inspector/project surfaces

Implementation:

1. Add Dock packages and a Dock factory/view-model layer.
2. Extract the major surfaces into reusable user controls:
   - project/assets/output
   - stage and monitor workspace
   - timeline workspace
   - inspector workspace
3. Host those surfaces in `DockControl` while keeping the top app menu and document strip.
4. Persist and restore the Dock layout at startup/shutdown.
5. Keep the current workspace commands mapped onto Dock actions.

Acceptance:

- the app uses Dock for the main panel workspace
- project, stage, timeline, inspector, and output can be docked and rearranged
- layout is restored between launches
- monitor and timeline surfaces remain functional after docking changes

## Execution Order

### Slice 1. Core model and service expansion

- add folder / parent / mute / solo metadata to `TimelineLayer`
- add audio finishing metadata to `LayerMediaSettings`
- add `LayerHierarchyService`
- add `PowerEditingService`
- add `EditorialEditingService`
- add `AudioMixService`

### Slice 2. Existing-shell UI integration

- wire hierarchy and folder actions into `MainWindowViewModel`
- update layer and frame row view models
- extend inspector and timeline menus for editorial and audio actions
- extend preview/export filtering for mute / solo and audio finishing

### Slice 3. Dock workspace refactor

- add Dock packages
- add a workspace factory and dockable panel view-models
- extract panel user controls from `MainWindow.axaml`
- host them in Dock and persist layout

### Slice 4. Verification

- extend unit tests for hierarchy, power editing, editorial edits, audio mixing, and serialization
- run build and tests
- smoke launch the app

## Risks and controls

| Risk | Control |
| --- | --- |
| Hierarchy breaks existing layer ordering | keep a normalization service and round-trip tests |
| Editorial tools corrupt clip timing | isolate logic in a dedicated service with bounds-based tests |
| Dock refactor destabilizes current commands | keep the top shell and bind dockable surfaces to the existing `MainWindowViewModel` |
| Audio finishing is not reflected in output | apply the same audio mix evaluation in preview and export services |

## Completion criteria

- `CrossToolParityRoadmap.md` phases 19-21 can be marked completed
- hierarchy, finishing, and Dock layout features are visible in the UI
- the project builds and tests pass
