# Flash XFL Advanced Authoring Analysis

Status: implemented on March 6, 2026.

## Baseline

Flash XFL interop already preserves:

- scenes, labels, markers, and transitions
- symbols, linkage metadata, shared-library metadata, and Scale-9 grids
- exact motion tracks, easing, and shape tweens
- compositing, filters, and media sync
- instance names and classic text styling
- packaged `.fla` archives with `DOMDocument.xml` and `LIBRARY/*.xml`

The remaining gaps are now in a smaller set of Animate authoring controls that
still affect how imported Flash content behaves in the editor.

## Remaining parity gaps

| Area | Current state | Remaining gap |
| --- | --- | --- |
| Motion guide workflow | Guide layers exist as a visual role only | No explicit guide-to-layer relationship, no guide options, and no XFL guide-binding metadata. |
| Text field authoring | Static text style survives | No static/dynamic/input text mode, no selectable/border/variable metadata, and no line-mode metadata. |
| Symbol registration | Symbols preserve linkage and Scale-9 | No registration-point metadata in the library or package round-trip. |

## Adobe references used

- [Motion tween authoring](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- [Classic tween authoring](https://helpx.adobe.com/animate/using/classic-tween-animation.html)
- [Timeline and layers](https://helpx.adobe.com/animate/using/timeline-layers.html)
- [Using symbol instances in Animate](https://helpx.adobe.com/id_id/animate/using/symbol-instances.html)
- [Classic text in Animate](https://helpx.adobe.com/si/animate/using/classic-text.html)
- [Scaling and caching symbols on the Stage](https://helpx.adobe.com/sg/animate/using/scaling-caching-symbols.html)

## Decision

Implement one more authoring-focused XFL parity pass that adds:

1. explicit motion-guide bindings
2. richer text-field authoring metadata
3. symbol registration-point metadata

## Target improvements

### 1. Motion guide metadata

Preserve:

- which layer is guided by which guide layer
- whether the instance orients to the guide path
- whether the instance snaps to the guide

Reason:

- Guide layers are an Animate editing workflow, not just a visual hint.
- Interop should preserve those relationships even when the app also has exact
  motion tracks.

### 2. Text field authoring modes

Preserve:

- static / dynamic / input field mode
- single-line / multiline / multiline-no-wrap mode
- selectable flag
- border/background flag
- bound variable name

Reason:

- Animate’s text workflow exposes these fields directly in Properties.
- Imported Flash content with dynamic/input text loses intent without them.

### 3. Symbol registration point metadata

Preserve:

- normalized symbol registration point X/Y

Reason:

- Registration points are core symbol authoring metadata in Animate.
- Even when the app continues to render using its current placement model, the
  metadata must survive round-trip for interop fidelity.
