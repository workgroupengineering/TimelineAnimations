# Adobe Animate FLX/XFL Parity Analysis

## Scope

This analysis focuses on the remaining Adobe Animate feature gaps that still matter for Flash-lineage (`FLA`/`XFL`) parity in this application.

`FLX parity` is treated here as **Flash/XFL authoring parity**, because Adobe Animate's editable authoring/document interchange surface is `FLA` and `XFL`; there is no separate modern Adobe Animate `FLX` authoring format.

## Baseline

The current app already covers a large portion of Animate-style authoring:

- scenes, symbols, nested timelines, button states
- keyframes, spans, motion presets, easing curves, shape tweens
- vector/path tools, drawing modes, primitive shapes
- filters, blend modes, masks, color effects
- component/library/media workflows
- publish targets and XFL round-trip support

The largest remaining fidelity gaps are no longer broad workflow gaps. They are **high-value authoring semantics** that Animate still exposes directly in the document model and the Property/Component/Sound workflows.

## Adobe Animate Capability Baseline

The following Adobe documentation defines the relevant parity targets:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Workspace workflow](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Authoring panels](https://helpx.adobe.com/si/animate/using/authoring-panels.html)
- [Create 3D graphics in Animate](https://helpx.adobe.com/animate/using/3d-graphics.html)
- [Motion tween animation](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- [How to use sound in Adobe Animate](https://helpx.adobe.com/animate/using/using-sounds.html)
- [Using Components in HTML5 Canvas](https://helpx.adobe.com/animate/using/using-components-in-animate-cc.html)
- [XFL format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)

## Gap Summary

### 1. 3D transform authoring is still incomplete

Adobe Animate still supports:

- 3D translation/rotation on movie clip instances
- document-level perspective angle
- document-level vanishing point
- transform panel fields for 3D rotation values
- tweenable 3D X/Y/Z rotation values in supported targets

Current app state:

- supports only 2D `X/Y/Width/Height/ScaleX/ScaleY/SkewX/SkewY/Rotation/Opacity`
- has no document-level perspective angle or vanishing point
- cannot author or preview 3D rotation/translation semantics in the inspector
- does not round-trip these values through XFL

Impact:

- imported Animate/XFL files lose 3D transform semantics
- rendering preview diverges from Animate for 3D-authored symbols
- timeline property coverage remains below Animate's motion editor surface

### 2. Flash-style sound sync semantics are still incomplete

Adobe Animate still exposes:

- `Event`
- `Start`
- `Stop`
- `Stream`
- repeat count
- loop forever
- stop-at-keyframe workflows

Current app state:

- supports `Start`, `Event`, and `Stream`
- supports `Loop`
- does not support explicit `Stop`
- does not support authorable finite repeat count
- does not model stop-keyframe semantics in XFL parity terms

Impact:

- Animate sound timelines do not round-trip fully
- lip-sync/editorial preview fidelity is incomplete
- publish/import behavior is weaker for long-form Flash audio authoring

### 3. Component parameter authoring is still incomplete

Adobe Animate exposes:

- reusable components
- property inspector editing for instances
- component parameters panel
- instance-specific parameter values
- component metadata that drives reusable stage behavior

Current app state:

- supports reusable component-style library items
- supports component insertion and behaviors
- does not define component parameter schemas
- does not store per-instance parameter overrides
- cannot substitute those values into nested symbol content/rendering
- does not round-trip component parameters through XFL

Impact:

- component reuse remains graphic-centric instead of parameter-driven
- instance editing is weaker than Animate's component workflow
- imported/exported component-heavy content loses semantic configurability

## Architectural Implications

The missing work spans every layer:

- `Core.Models`
  - new transform, media, and component-parameter metadata
- `Core.Services`
  - interpolation, media time resolution, component parameter resolution, XFL import/export, validation
- `App.ViewModels`
  - inspector state, summaries, commands, property track surfacing
- `App.Views`
  - dock inspector and project panels for 3D/sound/component editing
- `Rendering`
  - 3D-flattened preview parity and parameterized nested-symbol rendering
- `Tests`
  - model, service, XFL, and viewmodel regressions

## Recommended Phases

### Phase 31: 3D Transform Parity

Implement:

- `RotationX`, `RotationY`, `ZDepth` animated properties
- document-level perspective angle and vanishing point
- preview/render flattening from 3D authoring metadata into stage view
- inspector editing and timeline property surfacing
- XFL round-trip and target validation

### Phase 32: Sound Sync Parity

Implement:

- `Stop` playback mode
- repeat count and repeat-forever semantics
- timeline/media resolution that honors stop and finite repeat workflows
- inspector editing and XFL round-trip

### Phase 33: Component Parameter Parity

Implement:

- component parameter definitions on library items
- per-instance parameter overrides on placed component layers
- parameter substitution during nested symbol sampling/rendering
- project/inspector UI to edit definitions and overrides
- XFL round-trip for component metadata

## Success Criteria

The gap is bridged when all of the following are true:

- Animate/XFL imports preserve 3D, sound sync, and component-parameter semantics
- inspector and timeline UI can author those semantics directly
- renderer and stage preview reflect those semantics without code-behind logic
- XFL exports preserve the authored data
- regression tests cover the round-trip and editing behavior
