# Timeline Interaction Performance Plan

## Goal

Remove the remaining lag during frame-range dragging, ruler dragging, and curve
keyframe movement while keeping all current authoring features and parity.

## Current findings

### 1. Interactive drags still trigger heavyweight ViewModel refresh paths
- `CurrentTime` changes during interaction still route through shared editor update
  logic.
- Frame selection changes still update selection state twice per move
  (`SelectionStartFrame` and `SelectionEndFrame` each trigger refresh).
- Keyframe movement is lighter than before, but still shares generic interactive
  state with other edit flows.

### 2. Frame selection work is broader than necessary
- Each move updates all frame rows and all span selection state.
- Unchanged rows still do unnecessary span iteration.

### 3. Interaction mode is too generic
- `_isInteractiveChange` is a blunt switch.
- Keyframe drag, frame selection, frame ruler drag, and canvas transforms do not
  have different refresh budgets.

### 4. Control event streams still over-emit
- Frame controls now dedupe by frame/range/ruler.
- Curve controls still emit scrub/move updates without local dispatch guards.

## Optimization strategy

### Phase 1. Introduce interaction-mode aware refresh policy
- Add explicit interaction kinds:
  - keyframe drag
  - frame selection/ruler drag
  - generic canvas transform
- Use different UI refresh budgets per mode.
- During frame/curve drag, keep stage redraws live but defer non-essential
  inspector/program/audio/editor refresh until commit.

### Phase 2. Batch frame-selection updates
- Suppress per-property frame selection callbacks while both range endpoints are
  being updated.
- Refresh row selection and frame-selection state once per drag step.

### Phase 3. Make row updates incremental
- Optimize frame-row selection updates so non-selected rows avoid span walks.
- Keep track rows incremental during keyframe movement.

### Phase 4. Reduce control-side drag chatter
- Add local dispatch guards for curve scrub/keyframe move requests.
- Keep event emission aligned with meaningful target changes.

### Phase 5. Verify with focused regressions
- Add tests for:
  - interactive frame selection batching
  - interaction-mode-specific refresh behavior
  - incremental frame-row selection behavior
  - interactive keyframe drag preserving lightweight update flow

## Success criteria

- Frame-range drag remains responsive with no visible cursor/input stalls.
- Curve keyframe movement feels realtime again.
- Playback stays smooth while timeline surfaces are visible.
- All existing timeline features remain available.
