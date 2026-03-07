# Flash XFL Transform Parity Implementation Plan

## Status

Completed on March 6, 2026.

## Phase 1: Core model expansion

- Extend `AnimatedProperty` with `ScaleX`, `ScaleY`, `SkewX`, and `SkewY`.
- Extend `LayerDefaults` with matching base values.
- Extend `LayerSnapshot` with sampled transform values.
- Keep default values backward-compatible with existing documents.

## Phase 2: Sampling and editing services

- Update interpolation and frame sampling to carry the new properties.
- Update default-track creation, property updates, clipboard/range operations, and curve editing to include the new transform tracks.
- Preserve transform values when cloning layers, creating symbols, and relinking library items.

## Phase 3: Shared rendering path

- Replace rotation-only render helpers with a shared affine transform helper.
- Apply scale/skew/rotation consistently in:
  - stage preview
  - onion skin preview
  - monitor preview/export rendering
- Use library registration points as the transform origin for linked symbol instances.

## Phase 4: XFL import/export

- Export/import explicit `scaleX`, `scaleY`, `skewX`, and `skewY` values on visual elements.
- Preserve the same properties in explicit XFL property tracks.
- Keep import backward-compatible with older app-generated XFL that only stored width/height/rotation.

## Phase 5: Inspector and timeline wiring

- Add compact inspector fields for scale and skew.
- Surface the new properties in track labels and current-value displays.
- Keep selection, keyframe add/remove/move, and curve editing aligned with the new properties.
- Make canvas hit testing, selection handles, and motion anchors respect transform origin and affine transforms.

## Phase 6: Validation

- Add regression tests for:
  - transform property interpolation
  - XFL transform round-trip
  - registration-point-aware transform rendering behavior
  - inspector/timeline state refresh for the new properties
- Run build, tests, and a desktop smoke launch.

## Acceptance criteria

- The editor can author and animate scale/skew values directly.
- Stage preview and export rendering respect scale/skew and symbol registration-point transform origin.
- Flash XFL round-trip preserves the new transform properties and tracks.
- Existing documents and previously exported simplified XFL still load correctly.
