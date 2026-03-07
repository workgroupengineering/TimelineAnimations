# Flash XFL Transform Parity Analysis

## Status

Implemented on March 6, 2026.

## Baseline

Flash XFL interop already preserves:

- scenes, labels, markers, and transitions
- symbols, button states, linkage, registration points, and Scale-9 metadata
- guide bindings and text-field authoring metadata
- explicit property tracks, easing, shape tweens, compositing, and media sync
- packaged `.fla` save/load

The remaining visible parity gap is transform authoring fidelity.

## Remaining high-value gaps

| Area | Current state | Remaining gap |
| --- | --- | --- |
| 2D transform authoring | Position, width, height, rotation, and opacity are animated | No explicit scale or skew authoring model, so imported Animate content with non-rectangular transforms loses intent. |
| Symbol transform origin | Library items preserve registration points and stage placement uses them on insertion | Stage/export rendering still rotates around the visual bounds center instead of the symbol registration point. |
| XFL transform fidelity | XFL preserves the basic size/rotation values | No structured scale/skew metadata or transform-track round-trip for Animate-style transform workflows. |
| Inspector/timeline transform editing | Users can edit X/Y/Width/Height/Rotation/Opacity | No compact numeric editing or property-curve access for scale/skew properties. |

## Adobe references used

- [Animate XML / XFL file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Create a motion tween animation](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- [Classic tween animation](https://helpx.adobe.com/animate/using/classic-tween-animation.html)
- [Using symbol instances in Animate](https://helpx.adobe.com/id_id/animate/using/symbol-instances.html)

## Decision

Implement one more parity pass focused on transform fidelity:

1. add explicit scale X / scale Y / skew X / skew Y properties to the editor model
2. preserve them in XFL import/export and property tracks
3. render layers with a shared affine transform path in stage preview and export
4. use symbol registration points as the transform origin for linked instances
5. expose the new properties in the inspector and timeline/curve editors

## Why this slice matters

- It closes a real authoring gap between Animate and the current editor rather than adding passive metadata.
- It improves visual parity for imported Flash/XFL files that depend on skewed UI panels, flattened perspective cheats, or symbol-instance scaling.
- It strengthens both interchange and day-to-day editing because the same transform model is used by renderer, exporter, and curve editor.
