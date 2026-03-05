# TimelineAnimations Implementation Plan

## Architecture
- `src/TimelineAnimations.Core`
  - Timeline document models
  - Interpolation and editing services
  - JSON serialization
- `src/TimelineAnimations.App`
  - Avalonia desktop shell
  - View models for layers, tracks, and editor state
  - Custom controls for the canvas and timeline editor
  - Theme resources and interaction wiring
- `tests/TimelineAnimations.App.Tests`
  - Logic-focused tests against the core library

## Delivery Steps
1. Build the timeline core with deterministic interpolation and edit operations.
2. Create the editor shell with a custom visual system and multi-panel layout.
3. Implement the stage surface for selection, dragging, resizing, and drag-drop creation.
4. Implement the timeline surface for scrubbing, keyframe creation, selection, and movement.
5. Add inspector editing, playback, and document save/load flows.
6. Validate with build and test passes, then polish rough interaction edges.

## Interaction Model
- Scrubbing updates the live canvas preview.
- Direct canvas edits update current values and create keyframes when editing away from the origin or when auto-key is enabled.
- Timeline keyframe drags snap to a fixed time grid for precise edits.
- Layer selection is synchronized across layer list, canvas, timeline, and inspector.

## Visual Direction
- Dark cinematic workspace with warm and cool accent pairs.
- Rounded, layered surfaces rather than default control chrome.
- High-contrast typography and animated focus affordances.
