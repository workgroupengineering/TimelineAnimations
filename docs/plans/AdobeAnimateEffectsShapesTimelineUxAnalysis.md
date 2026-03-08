# Adobe Animate Effects, Shapes, Timeline, and UX Analysis

## Goal

Focus the next Adobe Animate parity pass on the authoring areas that most affect
day-to-day animation work:

- visual effects and filter controls
- primitive and vector shape workflows
- timeline property authoring depth
- stage and timeline UX affordances

This analysis assumes the broader Animate bridge phases are already implemented
and concentrates only on the remaining gaps in those four surfaces.

## Adobe Animate Baseline

The current Adobe Animate authoring surface for this pass was reviewed against:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Drawing tools in Animate](https://helpx.adobe.com/animate/using/drawing.html)
- [Painting tools in Animate](https://helpx.adobe.com/animate/using/painting-tools.html)
- [Motion tween animation](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- [Classic tween animation](https://helpx.adobe.com/animate/using/classic-tween-animation.html)
- [Shape tweening](https://helpx.adobe.com/animate/using/shape-tweening.html)
- [Timeline and layers](https://helpx.adobe.com/animate/using/timeline-layers.html)
- [ActionScript and code authoring](https://helpx.adobe.com/sk/animate/using/actionscript.html)

## Current App Strengths In This Area

The app already has a strong base:

- frame timeline plus property curves
- guide layers, camera layers, masks, blend modes, and color effects
- blur, glow, drop shadow, tint, brightness, saturation, and Flash native color modes
- direct stage drawing for rectangle, ellipse, line, pen, brush, text, eyedropper,
  paint bucket, and ink bottle
- shape tweening, motion paths, custom easing, and nested symbol workflows
- docked workspace with stage, source/program monitors, frames, curves, inspector,
  project/assets, and output/publish surfaces

## Gap Matrix

| Area | Adobe Animate capability | Current app status | Remaining gap |
| --- | --- | --- | --- |
| Filter stack | Blur, glow, drop shadow, bevel, gradient glow, gradient bevel, richer color adjustments | Blur, glow, drop shadow, tint, brightness, saturation, Flash native color modes | Missing bevel family controls, gradient filter variants, contrast, hue shift, and richer inspector grouping |
| Primitive shapes | Rectangle and oval primitive editing with deeper authored shape metadata | Basic rectangle and ellipse with single corner radius and gradients | Missing per-corner primitive radius editing, oval arc/pie editing, and authored shape mode metadata |
| Drawing modes | Merge Drawing, Object Drawing, and primitive-authoring semantics | Shapes are created as a single generic authored object kind | Missing explicit drawing-mode workflow, summary, and reuse in stage/inspector UX |
| Additional shape breadth | PolyStar / regular polygon workflows and richer primitive creation options | Rectangle, ellipse, line, pen, brush, text, path | Missing star/polygon authoring workflow |
| Timeline property reuse | Motion presets and reusable motion recipes | Timeline tracks exist, but there is no reusable preset surface | Missing capture/apply/delete motion preset workflow |
| Onion and frame display | Onion-skin options and stronger frame display controls | Before/after onion counts exist | Missing keyframe-only onion mode, opacity/falloff controls, and stronger visibility in the docked timeline |
| Animate-like quick UX | Tool-sensitive property strip and compact stage/timeline property affordances | Inspector covers most editing, but stage/timeline quick controls are still limited | Missing fast tool-property access for drawing mode, shape properties, and motion preset application |

## Priority Assessment

### Highest-value gaps

1. richer filter stack and color adjustments
2. primitive shape metadata and authored drawing modes
3. motion preset reuse and stronger timeline property affordances

### Moderate-value UX gaps

1. quick stage property strip for active drawing tools
2. better onion-skin controls in the timeline
3. clearer authored-shape summaries in inspector and project panels

## Recommended Bridge Strategy

### Phase 27. Effects Parity

Add the missing Animate-style effect controls that have the highest visual value:

- bevel
- gradient glow
- gradient bevel
- contrast
- hue shift

These must be implemented in:

- core compositing model
- stage rendering
- export rendering
- Flash XFL round-trip
- inspector UI in both shells

### Phase 28. Shapes Parity

Add authored shape metadata and workflows:

- drawing mode: merge, object, primitive
- per-corner rectangle radii
- oval start/sweep for arc and pie-like primitives
- poly/star authoring
- authored-shape summary shown in the UI

### Phase 29. Timeline Properties Parity

Add reusable motion property workflows and deeper onion/timeline controls:

- motion preset capture/apply/delete
- built-in motion preset starter library
- onion keyframes-only mode
- onion opacity and falloff controls
- timeline property summaries close to the curves editor

### Phase 30. Animate-Like Quick UX

Reduce travel between stage and inspector:

- compact quick-properties strip on the stage surface
- tool-sensitive controls for draw mode and primitive shape options
- quick motion preset application near curves/timeline

## Delivery Standard

Each phase is only complete when it lands in:

- models
- services
- viewmodels
- docked UI
- classic/fallback UI where equivalent surfaces still exist
- rendering/export
- tests

## Analysis Conclusion

The remaining Animate gap in the requested focus areas is no longer about broad
editor capability. It is concentrated in:

- richer filter controls
- deeper primitive shape authoring
- reusable motion property workflows
- faster tool-adjacent UX

Those gaps can be closed cleanly on the existing architecture without another
full workspace rewrite.

## Status

The bridge plan derived from this analysis was implemented in the current work
tree. The added scope covers:

- richer effect controls and rendering metadata
- primitive shape authoring depth and poly/star tools
- motion preset capture/apply workflows
- advanced onion-skin settings
- quick stage and timeline property UX
