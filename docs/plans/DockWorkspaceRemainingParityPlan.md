# Dock Workspace Remaining Classic Parity Plan

Status: in progress

## Goal

Finish the migration from the legacy/classic shell into the Dock workspace so day-to-day authoring no longer depends on the old inline layout for core editing affordances.

## Analysis

The large authoring panels are already ported:

- `Project` and `Assets` are docked in the project tool.
- `Properties` and `Motion + Actions` are docked in the inspector tool.
- `Interop` and `Publish` are docked in the output tool.

The remaining gaps are in the dock-native editing surfaces and workspace chrome:

1. Dock host command parity
   - The dock host still depends on the outer shell for document commands and editing-session actions.
   - Missing dock-native affordances:
     - open/save
     - frame/sequence/project export
     - undo/redo
     - sample reset
     - enter/exit symbol edit
     - prototype toggle
     - current context / selection context / shortcuts help

2. Stage dock parity
   - The docked stage renders correctly but still has a thinner control strip than the classic shell.
   - Missing or underexposed:
     - play/stop transport at the stage surface
     - current time / duration / viewport guidance
     - stronger stage-session status readout

3. Frames dock parity
   - The docked frames view currently exposes only a reduced subset of the classic timeline toolbar.
   - Missing or underexposed:
     - stop
     - add/delete current keyframe
     - auto-key
     - loop / play-all-scenes
     - snap toggle
     - copy/paste frame range
     - in/out/work-area controls
     - scene marker commands
     - onion-skin count commands
     - multiple-frames summary and classic range metrics

4. Curves dock parity
   - The docked curves view still exposes a minimal control row.
   - Missing or underexposed:
     - stop
     - auto-key
     - custom easing editor surface
     - shape tween capture/delete controls
     - editing-model guidance and richer keyframe summaries

## Implementation Phases

### Phase 1: Dock Host Command Strip

Add a dock-native command strip inside `DockWorkspaceHostView` for:

- open
- save
- current-frame export
- PNG-sequence export
- project export
- edit symbol
- exit symbol
- prototype
- undo
- redo
- sample reset
- context/help menu
- status / file summary chip

Implementation files:

- `src/TimelineAnimations.App/Views/Dock/DockWorkspaceHostView.axaml`
- `src/TimelineAnimations.App/Views/Dock/DockWorkspaceHostView.axaml.cs`
- `src/TimelineAnimations.App/Services/DockWorkspaceFileActionService.cs`

### Phase 2: Stage Surface Parity

Expand the docked stage header to include:

- play / stop
- current time and duration chips
- viewport hint / stage guidance
- retained fit / 1:1 / zoom overlay workflow

Implementation files:

- `src/TimelineAnimations.App/Views/Dock/DockStageDocumentView.axaml`

### Phase 3: Frames Surface Parity

Upgrade the docked frames toolbar to include the classic timeline affordances:

- transport and keyframe controls
- playback and options menus
- range, markers, and onion menus
- frame label editing
- frame range and scene-range summaries

Implementation files:

- `src/TimelineAnimations.App/Views/Dock/DockFramesToolView.axaml`

### Phase 4: Curves Surface Parity

Upgrade the docked curves workspace to include:

- transport and keyframe controls
- easing selection
- custom bezier easing editor
- shape tween controls
- editing model guidance

Implementation files:

- `src/TimelineAnimations.App/Views/Dock/DockCurvesToolView.axaml`

### Phase 5: Validation

Extend headless coverage to ensure the new dock-native strips render and remain discoverable.

Implementation files:

- `tests/TimelineAnimations.App.Tests/TimelineHeadlessTests.cs`

## Acceptance Criteria

- The dock workspace can be used without relying on the old classic panel chrome for common authoring actions.
- Frames and curves docks expose the same editing breadth as the classic shell toolbars.
- The dock host provides document/session actions and user guidance directly inside the docked workspace.
- Headless rendering coverage passes and the full solution test suite remains green.
