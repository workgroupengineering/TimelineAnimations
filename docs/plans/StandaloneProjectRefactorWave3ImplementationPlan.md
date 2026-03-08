# Standalone Project Refactor Wave 3 Implementation Plan

## Goal

Extract the reusable timeline authoring module into standalone projects without
changing user-visible behavior.

## Phase 1. Create a presentation assembly
Status: completed

Create `TimelineAnimations.Presentation` and move reusable presentation-layer
types into it:

- `ViewModelBase`
- `KeyframeViewModel`
- `FrameSpanViewModel`
- `FrameLabelViewModel`
- `SceneMarkerViewModel`
- `PropertyTrackViewModel`
- `LayerFrameTimelineRowViewModel`
- `TimelineTrackRowViewModel`
- `FrameTimelineRowKind`
- `TimelineTrackRowKind`
- `TimelineTrackGroupKind`
- `ColorHelpers`

Rationale:

- these types are not shell-specific
- timeline controls depend on them directly
- moving them removes one of the largest app-only reusable clusters

## Phase 2. Move reusable timeline controls into the controls assembly
Status: completed

Move these controls into `TimelineAnimations.Controls`:

- `FrameTimelineControl`
- `FrameTimelineInteractionEventArgs`
- `TimelineEditorControl`
- `TimelineEditorInteractionEventArgs`

Rationale:

- both classic and docked shells consume them
- they are reusable editor surfaces, not app-shell implementation details

## Phase 3. Rewire project references
Status: completed

Required wiring:

- `TimelineAnimations.Controls` references `TimelineAnimations.Presentation`
- `TimelineAnimations.App` references `TimelineAnimations.Presentation`
- solution includes the new project

## Phase 4. Validate extraction
Status: completed

Validation targets:

- solution build passes
- timeline-related focused tests pass
- app launches with both classic and docked timeline surfaces intact

## Implemented files

### New project

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/TimelineAnimations.Presentation.csproj`

### Presentation extraction

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/ViewModelBase.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/KeyframeViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/FrameSpanViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/FrameLabelViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/SceneMarkerViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/PropertyTrackViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/LayerFrameTimelineRowViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/ViewModels/TimelineTrackRowViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/Models/FrameTimelineRowKind.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/Models/TimelineTrackRowKind.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/Models/TimelineTrackGroupKind.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Presentation/Helpers/ColorHelpers.cs`

### Controls extraction

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Controls/Controls/FrameTimelineControl.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Controls/Controls/FrameTimelineInteractionEventArgs.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Controls/Controls/TimelineEditorControl.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Controls/Controls/TimelineEditorInteractionEventArgs.cs`

### Project wiring

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Controls/TimelineAnimations.Controls.csproj`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/TimelineAnimations.App.csproj`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`

## Deferred work

This wave is intentionally complete for the timeline module only. It does not
attempt to extract:

- `SceneCanvasControl`
- dock workspace host/factory/view models
- shell command/menu orchestration

Those require a broader host abstraction and should be handled in a separate
wave.
