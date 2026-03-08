# Adobe Animate Vector Tools Bridge Analysis

Status: completed
Owner: Codex
Date: 2026-03-08

## Goal

Bridge the remaining practical gap between TimelineAnimations Studio vector
tooling and Adobe Animate's vector authoring workflow, while preserving the
existing retained-path, tween, and XFL interoperability stack.

## Adobe reference baseline

Official Adobe references used for this audit:

- https://helpx.adobe.com/animate/using/draw-simple-lines-shapes.html
- https://helpx.adobe.com/bg/animate/using/drawing.html
- https://helpx.adobe.com/animate/using/reshape-lines-shapes.html
- https://helpx.adobe.com/ee/animate/using/working-with-paint-brush.html
- https://helpx.adobe.com/animate/using/creating-paint-brushes.html
- https://helpx.adobe.com/ae/animate/using/coloring.html
- https://helpx.adobe.com/sk/animate/using/changing-stroke-shape-color-animation-object.html

## Current baseline

The app already has:

- selection, subselection, lasso, hand, zoom, rotate view, eyedropper,
  paint bucket, ink bottle, warp, rig, rectangle, ellipse, poly-star, text,
  line, pen, brush, pencil, and eraser tools
- retained primitive metadata for rectangles, ellipses, and stars
- Bezier anchor and handle editing on stage
- path cleanup operations such as insert, delete, reverse, smooth,
  straighten, and optimize
- variable-width stroke rendering and point-level width data
- gradient angle, center, and scale support

## Remaining practical gaps

The biggest practical gaps are no longer raw path support. They are tool-depth
gaps:

1. no dedicated Width tool workflow
2. no reusable Adobe-style width profile application workflow
3. brush, pen, line, and pencil output still default to mostly uniform stroke
   width unless the user edits points manually afterward
4. Paint Bucket applies fill blindly and does not respect small/medium/large
   gap behavior
5. Lock Fill semantics are not exposed, so gradient fills do not preserve
   their transform across repeated fills the way Animate users expect
6. tool UI does not surface these vector-specific options in both docked and
   classic shells

## Gap matrix

| Area | Adobe Animate baseline | Current app | Gap severity |
| --- | --- | --- | --- |
| Width Tool | direct width-point editing on stroke | width only editable from selected anchor inspector | High |
| Width profiles | reusable variable-width profiles | no reusable profile surface | High |
| Brush capture | authored brush strokes can keep expressive width character | brush/pencil/pen mostly start uniform | High |
| Paint Bucket gaps | close small/medium/large gaps for fills | fill applies without gap analysis | High |
| Lock Fill | preserve fill transform while recoloring/filling | not exposed | Medium |
| Tool UX parity | vector-tool options live near the stage and tool inspector | partial | Medium |

## Bridge strategy

The remaining gap should be closed in four phases:

1. width-profile core services and tool-only enums
2. dedicated Width tool interaction on stage
3. gap-aware Paint Bucket and Lock Fill behavior
4. docked/classic UX parity plus validation

## Expected outcome

After this pass the app supports:

- Width-tool style drag editing for selected vector anchors
- reusable width profiles applied to existing paths and new brush/pen/pencil
  strokes
- Paint Bucket fill that only closes gaps within the selected tolerance
- Lock Fill preserving gradient transform on repeated fills
- matching vector-tool controls in docked and classic editor shells

## Implementation result

Completed in this pass:

- reusable width-profile services and localized width edits for selected path points
- dedicated Width tool stage interaction with drag-based width editing
- gap-aware Paint Bucket decisions with `None`, `Small`, `Medium`, and `Large`
- Lock Fill gradient-transform preservation
- docked and classic shell parity for Width / Paint Bucket / stroke-profile controls
- focused vector-tool regressions
