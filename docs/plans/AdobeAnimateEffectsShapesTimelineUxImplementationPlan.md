# Adobe Animate Effects, Shapes, Timeline, and UX Implementation Plan

## Objective

Close the focused Adobe Animate parity gaps for:

- effects
- shapes
- timeline properties
- quick authoring UX

## Schedule

| Phase | Scope | Status | Target in this pass |
| --- | --- | --- | --- |
| 27 | Effects parity: bevel, gradient glow/bevel, contrast, hue shift | completed | yes |
| 28 | Shape parity: drawing modes, primitive shape metadata, poly/star authoring | completed | yes |
| 29 | Timeline property parity: motion presets, advanced onion controls | completed | yes |
| 30 | Quick UX parity: stage/timeline quick-property strips and summaries | completed | yes |

## Phase 27. Effects Parity

### Deliverables

- extend compositing with:
  - bevel settings
  - gradient glow settings
  - gradient bevel settings
  - contrast
  - hue shift
- render the new effects in stage preview and export
- persist and round-trip the new settings through Flash XFL and native project JSON
- expose the settings in docked and classic inspector surfaces

### Core changes

- extend `LayerCompositeSettings`
- extend color/effect helpers
- extend XFL filter import/export

### UI changes

- grouped inspector cards for:
  - color adjust
  - bevel
  - gradient glow
  - gradient bevel

### Acceptance

- effects update live on stage
- exported frames show the same effect stack
- Flash XFL round-trips the added effect metadata

## Phase 28. Shapes Parity

### Deliverables

- add authored drawing mode metadata:
  - merge
  - object
  - primitive
- add primitive rectangle corner radii:
  - top-left
  - top-right
  - bottom-right
  - bottom-left
- add primitive oval start/sweep controls
- add poly/star creation on the stage
- surface authored-shape summaries in inspector and layer metadata

### Core changes

- extend `LayerStyle`
- add primitive shape helpers/services
- update layer creation and canvas draw requests

### UI changes

- shape section in the inspector
- quick tool properties for primitive settings

### Acceptance

- users can create star/polygon shapes
- primitive rectangle and oval edits update live on stage
- drawing mode is visible and persisted

## Phase 29. Timeline Property Parity

### Deliverables

- add reusable motion preset capture/apply/delete
- seed a small built-in preset set
- add advanced onion settings:
  - keyframes-only
  - base opacity
  - falloff
- surface timeline summaries near the frames/curves controls

### Core changes

- add a motion preset model and service
- attach presets to `TimelineDocument`
- extend stage onion rendering behavior

### UI changes

- motion preset controls in curves/timeline surfaces
- onion settings in frames timeline controls

### Acceptance

- a layer motion preset can be captured and applied to another layer
- onion settings change stage preview immediately

## Phase 30. Quick UX Parity

### Deliverables

- add a compact stage quick-properties strip
- make quick controls tool-sensitive
- surface drawing mode, primitive options, and preset apply actions without opening deep inspector sections

### Acceptance

- common shape and motion actions can be performed from the stage/timeline surfaces
- the inspector remains the deep-edit surface, but not the only practical path

## Implementation Order

1. Phase 27
2. Phase 28
3. Phase 29
4. Phase 30

## Quality Gates

- `dotnet build TimelineAnimations.sln`
- targeted tests for:
  - effect metadata round-trip
  - color/effect rendering helpers
  - primitive shape creation and editing
  - motion preset capture/apply
  - onion advanced settings
- smoke launch of the desktop app

## Implementation Status

All four phases were implemented in this pass across:

- core models and services
- ViewModel state and commands
- docked workspace surfaces
- fallback shell wiring
- stage/export rendering
- Flash XFL round-trip
- regression tests

## Validation Status

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`: passed
- targeted tests for the new feature set: passed
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`: `135/139` passing

Known remaining failures are not from this pass. They are the existing Avalonia
headless `fonts:SystemFonts` failures in four dock-rendering tests:

- `DockProjectToolView_Renders_Project_And_Assets_Workspace`
- `DockInspectorToolView_Renders_Classic_Property_And_Action_Surfaces`
- `DockOutputToolView_Renders_Interop_And_Publish_Workspace`
- `DockCurvesToolView_Renders_ExpandedCurveEditorSurface`
