# Canvas Workspace Parity Plan

Status: completed on March 6, 2026.

## Goal

Bring the editing-area organization closer to Adobe Premiere Pro, Macromedia Flash /
Adobe Animate, and other timeline-heavy authoring tools by making the main canvas
surfaces first-class dockables instead of nested subpanes.

This pass is focused on workspace structure and editing-area usage:

- better separation of stage, source monitor, program monitor, frames, and curves
- stronger Dock-for-Avalonia layout presets for different workflows
- direct show / hide / auto-hide management for major editing surfaces
- more usable split arrangements for animation, editorial review, and monitor-heavy work
- clearer guidance that panels can be moved inline by dragging Dock tabs

## Current UX Gaps

### 1. Editing surfaces are bundled too coarsely

Current state:

- stage, source monitor, and program monitor are combined into one document
- frames and curves are combined into one timeline dockable
- users can switch views inside a panel, but cannot rearrange those surfaces like
  distinct editing work areas

Parity target:

- stage canvas is its own primary document
- source and program monitors are own document surfaces
- frame timeline and curve timeline can be arranged independently

### 2. Workspace presets are too shallow

Current state:

- the classic shell supports focus and overlay presets
- the Dock workspace only exposes a reset action and generic Dock drag behavior

Parity target:

- named studio presets with clear intent:
  - `Animate`
  - `Editorial`
  - `Review`
  - `Stage Focus`

### 3. Show / hide / overlay is not organized around real editing surfaces

Current state:

- classic workspace panels can be docked / overlayed / hidden
- the Dock workspace relies on generic tab drag and context menus

Parity target:

- explicit visibility and auto-hide controls for:
  - project
  - stage
  - source
  - program
  - frames
  - curves
  - inspector
  - output

### 4. Canvas-area usage is not guided enough

Current state:

- the app has the right building blocks, but not enough workspace-level guidance

Parity target:

- canvas and monitor areas should read as a professional editing desk
- the workspace should explain the current preset and the intended drag behavior
- stage, source, and program need balanced proportions and stable split arrangements

## Implementation Phases

### Phase 1. Split Editing Surfaces Into First-Class Dockables

Deliver:

- separate dockables and views for:
  - stage canvas
  - source monitor
  - program monitor
  - frames timeline
  - curves timeline
- retain the same `MainWindowViewModel` host orchestration

Acceptance:

- the major editing surfaces are docked independently
- users can drag them inline through Dock tabs and splits

Result:

- implemented with dedicated dockables for stage, source, program, frames, and curves

### Phase 2. Add Dock Workspace Presets

Deliver:

- add a dock-workspace preset model
- implement deterministic layouts for:
  - `Animate`
  - `Editorial`
  - `Review`
  - `Stage Focus`
- rebuild the Dock layout from the selected preset

Acceptance:

- switching presets materially changes panel arrangement
- presets map to real authoring modes rather than cosmetic changes

Result:

- implemented with `Animate`, `Editorial`, `Review`, and `Stage Focus` layouts

### Phase 3. Add Surface Visibility And Auto-Hide Controls

Deliver:

- add quick actions for each major dockable:
  - show / activate
  - hide
  - auto-hide
- expose those actions in the dock workspace strip
- keep compatibility with Dock drag, float, and inline rearrangement

Acceptance:

- users can manage editing surfaces without hunting through tab context menus
- auto-hide acts as the Dock-backed equivalent of overlay behavior

Result:

- implemented with explicit show/focus, hide, overlay, and reveal-all actions in the workspace organizer

### Phase 4. Improve Workspace Header And Guidance

Deliver:

- replace the minimal reset strip with a real workspace organizer
- show active preset, layout intent, and drag hint
- group controls by:
  - preset
  - monitors
  - timeline
  - panels

Acceptance:

- the dock workspace reads as a deliberate studio organizer
- users understand how to rearrange and restore panels

Result:

- implemented with a compact organizer strip, preset controls, quick surface buttons, grouped monitor/timeline/panel menus, and drag guidance

### Phase 5. Validation

Deliver:

- build and test coverage for preset creation and dockable visibility operations
- smoke launch after layout changes

Acceptance:

- solution builds cleanly
- tests remain green
- dock workspace loads with the new preset layouts

Result:

- `dotnet build TimelineAnimations.sln` passed
- `dotnet test TimelineAnimations.sln --no-build` passed with 90/90 tests
- `dotnet run --project src/TimelineAnimations.App --no-build` smoke-launched successfully

## Target Presets

### Animate

- project left
- stage center
- frames bottom
- curves tabbed with frames
- inspector and output right
- monitors available but not dominant

### Editorial

- stage large left
- source top-right
- program bottom-right
- frames bottom
- curves available beside frames
- inspector right tab group

### Review

- source left
- program center
- stage tabbed but secondary
- timeline bottom
- output / publish right

### Stage Focus

- stage dominant center
- monitors available as tabs
- timeline present but compact
- secondary panels easy to auto-hide

## Success Criteria

- stage, source, program, frames, and curves are all independently dockable
- workspace presets feel meaningfully different and task-specific
- show / hide / auto-hide actions are explicit for the major editing surfaces
- users can move surfaces inline using Dock tabs and split zones
- the editor feels closer to a professional animation/editorial workspace
