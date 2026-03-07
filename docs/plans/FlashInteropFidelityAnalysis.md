# Flash Interop Fidelity Analysis

## Baseline

Flash XFL import/export is now present, but the first pass focused on getting
editable scenes, layers, symbols, labels, markers, media placeholders, and basic
 transform round-trip working.

That leaves several Animate-era interop gaps:

| Area | Current state | Gap |
| --- | --- | --- |
| Motion tween fidelity | Exported as frame samples | Exact keyframe timing, easing, and shape tween metadata are not preserved explicitly. |
| Layer effects | Editor model supports them | Blend mode, blur, glow, shadow, tint, brightness, saturation, and mask metadata are not mapped into Flash interchange. |
| Media sync | Editor model supports Start/Event/Stream plus gain/pan/fades | XFL export only preserves a shallow media reference and clip timing. |
| Flash package workflow | Import accepts packaged `.fla`-style ZIP content | Save/export still writes only a plain `.xfl` XML stream. |

## Reference features from Adobe Animate docs

- [Animate XML / XFL file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Animate workspace workflow](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Motion tween authoring](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- [Classic tween authoring](https://helpx.adobe.com/animate/using/classic-tween-animation.html)
- [Shape tweening](https://helpx.adobe.com/animate/using/shape-tweening.html)
- [Timeline and layers](https://helpx.adobe.com/animate/using/timeline-layers.html)

## Decision

Add a second-pass XFL fidelity layer that preserves exact authoring semantics from
the current app model whenever a Flash/XFL concept exists or can be represented as
structured XFL-side metadata.

## Targeted improvements

### 1. Exact motion metadata

Preserve:

- property tracks
- exact keyframe times
- easing kind
- custom bezier control points
- shape keyframes

Reason:

- Sampling keeps playback visually similar, but it destroys authoring semantics.
- Import should prefer exact track metadata over reconstructing tracks from baked
  per-frame snapshots.

### 2. Flash-style layer effects and compositing

Preserve:

- layer type and mask count
- blend mode
- blur
- glow
- shadow
- tint
- brightness
- saturation
- parallax depth and camera role where relevant

Reason:

- These properties already exist in the editor model.
- Losing them on XFL round-trip makes Flash interchange materially incomplete.

### 3. Media sync fidelity

Preserve:

- playback mode (`Start`, `Event`, `Stream`)
- clip start
- clip offset
- clip duration
- loop
- volume
- gain
- pan
- fade in / fade out

Reason:

- Flash/Animate timelines are often sound-driven.
- This is already represented in the app and should survive XFL interchange.

### 4. Packaged Flash save/load

Add:

- zipped Flash package save path for `.fla`
- zip package still rooted by `DOMDocument.xml`
- package import remains backward-compatible with plain `.xfl`

Reason:

- The app already reads package-style input.
- Saving package-style output closes a major workflow gap.
