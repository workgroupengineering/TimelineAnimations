# Flash XFL Shape Style Parity Analysis

## Status

Implemented on March 7, 2026.

## Baseline

Flash XFL interop already preserves:

- scenes, symbols, library linkage, Scale-9 grids, registration points, and packaged archives
- transform tracks, easing, shape tweens, guide bindings, and Flash color effects
- text field metadata, cache/outline metadata, media sync, and compositing filters

The remaining high-value authoring gap is Flash shape styling fidelity.

## Remaining parity gaps

| Area | Current state | Remaining gap |
| --- | --- | --- |
| Gradient type | The app supports solid or a simple two-stop gradient | Animate-authored linear versus radial gradients are flattened into one internal gradient mode. |
| Gradient direction | The renderer always uses a fixed diagonal gradient direction | XFL-authored gradient angle is not preserved, so imported art can render differently and the editor cannot author the same intent. |
| Stroke caps and joins | Stroke thickness and color are preserved | Animate line cap, line join, and miter-limit settings are not represented explicitly, so open paths and sharp corners lose styling parity. |
| Fill/stroke visibility | Layers always assume a fill and stroke color string | XFL styles that explicitly suppress fill or stroke are flattened into default colors instead of author intent. |
| Editor parity | The inspector can edit fill, stroke, and a basic gradient toggle | There is no Flash-style section for gradient type/angle, fill/stroke visibility, or stroke edge behavior. |

## Adobe references used

- [Animate XML / XFL file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Create gradients in Animate](https://helpx.adobe.com/animate/using/color-panel.html)
- [Drawing lines and shapes in Animate](https://helpx.adobe.com/animate/using/drawing.html)
- [Extending Adobe Animate CC](https://www.adobe.com/content/dam/acom/en/devnet/flash/articles/extending_animate_cc.pdf)

## Decision

Implement a dedicated shape-style parity pass that adds:

1. explicit gradient kind and angle metadata
2. explicit fill/stroke visibility metadata
3. explicit stroke cap, join, and miter-limit metadata
4. XFL import/export mapping for that metadata
5. stage/export rendering and inspector editing parity

## Why this slice matters

- It fixes authoring differences users immediately notice on imported Flash artwork.
- It improves trust between imported XFL, stage preview, and exported output.
- It closes another real Animate workflow gap without inventing app-specific metadata.
