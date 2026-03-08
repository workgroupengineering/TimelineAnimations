# Adobe Animate FLX/XFL Parity Implementation Plan

## Objective

Close the remaining Flash-lineage parity gaps against Adobe Animate by implementing:

1. 3D transform parity
2. sound sync/repeat parity
3. component parameter parity

## Schedule

### Phase 31. 3D Transform Parity

Status: `completed`

#### Work items

- Extend animation property model:
  - add `RotationX`
  - add `RotationY`
  - add `ZDepth`
- Extend layer defaults and sampled snapshots
- Extend document Animate settings:
  - perspective angle
  - vanishing point X/Y
  - 3D authoring enablement
- Extend interpolation and property-track mapping
- Extend renderer/stage transform flattening using document 3D settings
- Extend inspector UI:
  - 3D rotation X/Y
  - Z depth
  - document perspective controls
- Extend XFL import/export
- Add validation warnings for unsupported targets
- Add tests for:
  - interpolation defaults
  - XFL round-trip
  - renderer flattening invariants
  - inspector/viewmodel state

### Phase 32. Sound Sync Parity

Status: `completed`

#### Work items

- Extend media playback model:
  - add `Stop`
  - add `RepeatCount`
  - add `RepeatForever`
- Update media timing service
- Update audio meter/mix semantics
- Extend inspector UI for repeat/stop workflows
- Extend XFL import/export for sync and repeat metadata
- Add tests for:
  - stop-keyframe timing
  - finite repeat timing
  - XFL round-trip

### Phase 33. Component Parameter Parity

Status: `completed`

#### Work items

- Add component parameter definition model
- Add per-instance override model
- Extend library items and timeline layers
- Add parameter resolution service
- Apply parameter substitution during nested symbol rendering
- Extend project panel for parameter definitions
- Extend inspector for instance overrides
- Extend XFL import/export
- Add tests for:
  - definition normalization
  - default/override resolution
  - render substitution
  - XFL round-trip

## Completion Rules

Each phase is only complete when:

- models, services, and UI are wired end to end
- XFL import/export covers the new metadata
- targeted tests pass
- the app builds successfully

## Implementation Notes

- Keep UI logic in ViewModels; no code-behind event handling
- Keep domain logic Avalonia-free
- Reuse existing inspector refresh/history flows
- Prefer explicit model fields over stringly typed metadata blobs

## Validation Status

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
  - passed
- targeted FLX/XFL parity regressions
  - passed
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
  - `137/142` passing
  - remaining failures are the pre-existing Avalonia headless `fonts:SystemFonts` dock-rendering tests plus one dock-layout file-contention test, outside this parity slice
