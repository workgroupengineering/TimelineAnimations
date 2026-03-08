# Adobe Animate Rendering Parity Analysis

## Objective

Close the remaining rendering-specific gaps between TimelineAnimations Studio and
Adobe Animate, with emphasis on:

- blend mode fidelity
- filter metadata fidelity
- runtime render behavior matching authored XFL/Animate settings
- stage/export consistency

## Reference Baseline

Official Adobe references used for this audit:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Using and applying blend modes](https://helpx.adobe.com/animate/using/applying-blend-modes.html)
- [Using graphic filters in Animate](https://helpx.adobe.com/animate/using/graphic-filters.html)
- [Create 3D graphics in Animate](https://helpx.adobe.com/animate/using/3d-graphics.html)

## Current Strengths

The app already had a strong rendering base before this pass:

- stage and export share the same renderer
- masks, camera, and parallax are implemented
- Flash color effects are implemented
- blur, glow, shadow, bevel, gradient glow, and gradient bevel are rendered
- XFL import/export already preserves a broad set of authoring metadata
- 3D document settings and 3D layer properties exist across the model and UI

## Gap Audit

| Area | Adobe Animate behavior | Previous app state | Gap |
| --- | --- | --- | --- |
| Blend modes | Animate exposes `Normal`, `Layer`, `Darken`, `Multiply`, `Lighten`, `Screen`, `Overlay`, `Hard Light`, `Add`, `Subtract`, `Difference`, `Invert`, `Alpha`, `Erase` | Only `Normal`, `Add`, `Screen`, `Multiply`, `Overlay` had runtime blending behavior | Missing render-time parity for most Adobe blend modes |
| Alpha / Erase semantics | `Alpha` acts as an alpha mask and `Erase` removes base pixels | Enum values did not exist or were not handled by renderer | Missing authored-result parity |
| Filter quality | Animate filters preserve quality levels (`low`, `medium`, `high`) and quality affects visual spread | XFL export/import flattened most filter quality values | Missing XFL fidelity and render-time sampling parity |
| Knockout / hide-object | Animate glow, shadow, bevel, gradient glow, and gradient bevel can hide the base object or render knockout-like output | Metadata was not fully modeled or rendered | Missing XFL fidelity and missing render behavior |
| Blend mode XFL strings | Animate uses stable XFL blend-mode tokens | Export relied on enum string shape | Lower interoperability fidelity than Adobe-authored XFL |
| Renderer validation | Adobe-style feature coverage should be regression-tested at import/export and render level | Existing tests covered broad filters, but not quality/knockout or the extended blend set | Coverage gap |

## Findings

The main remaining rendering parity gaps were concentrated in two places:

1. `CompositeFrameRenderer`
   - incomplete blend-mode coverage
   - no alpha/erase mask semantics
   - effect passes ignored filter quality metadata
   - knockout/hide-object flags did not affect base-object rendering

2. `FlashXflExchangeService`
   - native filter export/import did not fully preserve quality and knockout/hide-object flags
   - custom compositing metadata did not mirror those fields
   - blend mode serialization was not normalized to Adobe-style XFL tokens

## Target End State

After the parity pass:

- all Adobe Animate blend modes listed above render in stage/export
- `Alpha` and `Erase` behave as masking/erasing blend modes
- XFL round-trip preserves filter quality and knockout/hide-object metadata
- renderer uses quality-aware sampling for blur/glow/shadow/bevel families
- exported XFL uses stable Adobe-style blend-mode strings
- focused tests cover both XFL fidelity and renderer output

## Scope Decision

This pass is intentionally focused on rendering parity. It does not attempt to
re-implement unrelated authoring workflows or undocumented private Adobe file
internals. The goal is to close the concrete rendering-fidelity gaps found in
the current codebase and make authored output behave much closer to Animate.
