# Adobe Animate Rendering Parity Implementation Plan

## Goal

Implement the remaining Adobe Animate rendering parity gaps identified in
[AdobeAnimateRenderingParityAnalysis.md](./AdobeAnimateRenderingParityAnalysis.md).

## Schedule

| Phase | Scope | Status |
| --- | --- | --- |
| 44 | Blend mode runtime parity | Completed |
| 45 | Filter quality and knockout/hide-object fidelity | Completed |
| 46 | XFL token normalization and focused regression coverage | Completed |

## Phase 44: Blend Mode Runtime Parity

### Tasks

- extend `LayerBlendMode` to the full Animate render set
- implement runtime formulas in `CompositeFrameRenderer`
- add explicit `Alpha` and `Erase` masking behavior

### Deliverables

- full blend-mode enum coverage
- renderer support for `Layer`, `Darken`, `Lighten`, `Difference`,
  `HardLight`, `Subtract`, `Invert`, `Alpha`, and `Erase`

## Phase 45: Filter Quality And Knockout Fidelity

### Tasks

- add explicit filter quality model metadata
- preserve quality and knockout/hide-object flags through XFL import/export
- make effect sampling count depend on quality
- hide the base object when Animate-style knockout/hide-object settings require it

### Deliverables

- `LayerFilterQuality`
- quality-aware effect sampling
- knockout/hide-object-aware base-layer rendering
- XFL filter round-trip for those fields

## Phase 46: XFL Normalization And Validation

### Tasks

- export Adobe-style blend mode tokens instead of raw enum names
- parse both Adobe-authored and legacy app-authored blend strings
- add focused tests for:
  - extended blend rendering
  - alpha/erase mask behavior
  - XFL filter quality and knockout/hide-object round-trip

### Deliverables

- normalized XFL blend tokens
- render-level regression tests
- XFL fidelity regression tests

## Result

The rendering parity pass is complete for the gaps identified in this audit:

- Adobe Animate blend-mode coverage is implemented in the renderer
- Alpha/Erase masking behavior is implemented
- filter quality and knockout/hide-object metadata round-trip through XFL
- quality-aware sampling is implemented in the renderer
- focused tests protect both the XFL and renderer paths
