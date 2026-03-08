# Adobe Animate Vector Editing Parity Analysis

Status: completed analysis, implemented bridge phases
Owner: Codex
Date: 2026-03-08

## Goal

Close the remaining vector-authoring gap between TimelineAnimations Studio and
Adobe Animate, with emphasis on path editing, shape styling, stroke behavior,
gradient transforms, and stage editing UX.

## Reference baseline

Official Adobe sources used for the analysis:

- [Draw and create objects with Animate](https://helpx.adobe.com/bg/animate/using/drawing.html)
- [Draw lines and shapes with Adobe Animate](https://helpx.adobe.com/animate/using/draw-simple-lines-shapes.html)
- [Reshape lines and shapes](https://helpx.adobe.com/animate/using/reshape-lines-shapes.html)
- [Edit the motion path of a tween animation in Adobe Animate](https://helpx.adobe.com/animate/using/editing_the_motion_path_of_a_tween_animation.html)
- [Creating and managing Paint brushes](https://helpx.adobe.com/animate/using/creating-paint-brushes.html)
- [Using Paint Brush in Animate](https://helpx.adobe.com/ee/animate/using/working-with-paint-brush.html)
- [Transform and combine graphic objects in Animate](https://helpx.adobe.com/si/animate/using/transforming-combining-graphic-objects.html)
- [Shape tweens](https://helpx.adobe.com/my_ms/animate/using/shape-tweening.html)

## Adobe Animate vector-authoring baseline

Adobe Animate’s vector workflow includes:

1. direct point editing with anchors, corners, smooth points, and Bezier handles
2. object drawing versus merge drawing semantics
3. primitive rectangles, ovals, and stars with retained shape metadata
4. variable-width strokes and width-point editing
5. gradient transform editing beyond color stops and angle
6. cleanup/reshape operations such as smooth and straighten
7. path-oriented animation editing where handles and curves remain editable
8. vector brush workflows that preserve expressive strokes

## Current app baseline before this pass

Before this parity pass, the app already had:

- merge/object-style shape authoring modes
- rectangle, ellipse, star, pen, brush, pencil, lasso, eraser, and subselect tools
- retained primitive metadata
- path layer rendering and shape tweening
- gradient fills, stroke cap/join, and path point movement

The main gaps were:

- path points were effectively polyline-only in authoring and rendering
- no selected-point editing surface in the inspector
- no direct Bezier handle editing on stage
- no variable-width stroke metadata on points
- no gradient-center or gradient-scale authoring
- no path cleanup commands comparable to Animate’s vector cleanup workflows

## Gap matrix

| Area | Adobe Animate baseline | Pre-pass app state | Gap severity |
| --- | --- | --- | --- |
| Anchor editing | Subselection exposes anchors and Bezier handles | Anchor move only | High |
| Corner/smooth points | Editable point type | No explicit point type editing | High |
| Bezier rendering | Curved path display and editing | Straight segment display | High |
| Width tool behavior | Per-point width variation on strokes | Uniform stroke only | High |
| Gradient transform | Gradient center/scale editing | Angle + colors only | Medium |
| Shape cleanup | Smooth / straighten / optimize workflows | Not surfaced for paths | Medium |
| Shape tween fidelity | Variable-width and curved path interpolation | Basic point interpolation | High |
| Inspector UX | Point summary + point/property editing | Minimal `Close path` only | High |

## Bridge strategy

The parity work was split into four implementation phases:

1. Bezier-capable path data and rendering
2. Direct point/handle editing and selected-point inspector state
3. Variable-width strokes, gradient transform controls, and cleanup commands
4. Regression coverage and cross-shell parity

## Outcome of this pass

This pass implemented the concrete missing capabilities required for practical
Adobe Animate-style vector editing parity:

- Bezier handles in the path model
- on-stage handle dragging with Subselect
- selected-point state and inspector editing
- point handle-mode editing
- per-point stroke width scale
- gradient center and scale authoring
- insert/delete/reverse/smooth/straighten/optimize path commands
- curved path and variable-width rendering in both stage preview and export
- interpolation and persistence of the new vector metadata

## Remaining note

This closes the main authoring gap for retained vector path editing. The only
remaining vector-adjacent depth that could still be extended later is higher-end
brush-library authoring and a dedicated Width-tool mode UI, but the underlying
data/render pipeline added in this pass already supports the critical parity
features needed for authoring, rendering, and tweening.
