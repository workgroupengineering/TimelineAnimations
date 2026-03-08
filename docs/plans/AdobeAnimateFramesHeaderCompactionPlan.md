# Adobe Animate Frames Header Compaction Plan

## Objective

Refactor the top of the docked `Frames` panel so it behaves more like Adobe
Animate's timeline chrome: compact primary controls, lightweight secondary
options, and more vertical space reserved for the actual timeline lanes.

## Adobe Animate layout cues

Animate keeps the timeline header dense and task-focused:

- transport and keyframe actions stay in a compact top strip
- timeline state such as current frame, work area, and markers is shown as small
  status regions rather than large stacked cards
- less frequently changed onion-skin detail settings are available contextually,
  instead of consuming a permanent full-width row
- the frame ruler and lanes remain the visual priority

Reference baseline:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Timeline layers](https://helpx.adobe.com/animate/using/timeline-layers.html)

## Current issues

1. The header consumes three full rows before the actual frame ruler begins.
2. Onion-skin detail controls always occupy a dedicated row, even when the user
   only needs the toggle and summary.
3. Status for frame, in/out, work area, marker, and power mode is visually
   fragmented into large blocks.
4. Layout primitives are repeated inline instead of being organized through a
   reusable compact header component.

## Implementation plan

### Phase 1. Create reusable compact timeline summary control

Status: `completed`

- add a reusable compact stat chip control for timeline header/status usage
- add dense timeline-header styles for compact action buttons, compact entry
  fields, and grouped toolbar clusters

### Phase 2. Compress the frames header into two bands

Status: `completed`

- keep transport, keyframe, playback, snapping, and frame zoom on the first row
- keep label editing and status summaries on the second row
- keep `Insert`, `Range`, `Edit`, and `Markers` in a grouped toolbar cluster
- retain direct `Razor` access without dedicating a full row to it

### Phase 3. Move onion detail controls into contextual UI

Status: `completed`

- keep the onion toggle always visible
- move onion range and opacity/falloff details into a flyout
- keep onion summary visible in compact form when onion is enabled

### Phase 4. Validate layout and regressions

Status: `completed`

- build the solution
- run the focused frames dock headless test
- smoke-launch the app to verify the compact header layout

## Validation

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineAnimations.App.Tests.csproj --no-build --filter "DockFramesToolView_Renders_ExpandedTimelineToolbar"`
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`
