# Adobe Animate Feature Analysis

## Goal

Analyze Adobe Animate's current authoring surface against `TimelineAnimations Studio`,
identify the remaining user-facing gaps, and define the missing features that matter
for practical authoring parity.

This analysis is focused on:

- authoring workflow and document model
- drawing and stage tools
- timeline and interaction authoring
- publish target semantics
- modern rigging and deformation workflows

It does not claim binary or runtime parity with every Adobe export/runtime target.

## External Reference Baseline

The current Adobe Animate feature surface for this analysis was reviewed against:

- Adobe Animate product page:
  - [Adobe Animate](https://www.adobe.com/pl/products/animate.html)
- Adobe Animate workspace and authoring workflow:
  - [Workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- document types and target workflows:
  - [HTML5 Canvas documents](https://helpx.adobe.com/ie/animate/using/HTML5-Canvas.html)
  - [ActionScript 3.0 documents](https://helpx.adobe.com/sk/animate/using/actionscript.html)
  - [Create a WebGL document](https://helpx.adobe.com/ie/animate/using/create-webgl-documents.html)
  - [Convert projects to other document formats](https://helpx.adobe.com/ie/animate/using/converting-projects-to-other-document.html)
- drawing and paint tools:
  - [Drawing tools in Animate](https://helpx.adobe.com/animate/using/drawing.html)
  - [Painting tools in Animate](https://helpx.adobe.com/animate/using/painting-tools.html)
  - [Selection, lasso, and subselection tools](https://helpx.adobe.com/animate/using/selecting-objects.html)
- scripting workflow:
  - [Code Snippets panel](https://helpx.adobe.com/animate/using/code-snippets-panel.html)
  - [Actions panel](https://helpx.adobe.com/animate/using/writing-scripts.html)
- modern rigging and deformation:
  - [Layer parenting in Animate](https://helpx.adobe.com/animate/using/layer-parenting-in-adobe-animate.html)
  - [Asset Warp tool](https://helpx.adobe.com/sk/animate/using/asset-warp-tool.html)
  - [Modern rigging with Flexi Bone](https://helpx.adobe.com/bg/animate/using/modern-rigging-felxibone-animation-tool.html)

## Current App Strengths

The current app already covers a large part of the historic Flash/Animate core:

- scenes and multi-scene playback
- library/symbol/component authoring
- nested symbol editing
- frame spans, keyframes, labels, markers, and property curves
- easing, shape tweening, motion paths, and compositing
- media clips, monitors, transitions, and audio finishing
- visual states and behavior/action authoring
- publish/export workflows
- XAML, HTML, SVG, and Flash XFL interchange
- docked workspace with monitors, timeline, inspector, and asset panels

## Gap Matrix

| Area | Adobe Animate capability | Current app status | Gap |
| --- | --- | --- | --- |
| Document type semantics | Distinct document targets such as `HTML5 Canvas`, `WebGL`, and `ActionScript 3.0` with target-aware authoring and conversion | Generic document and publish profile model exists | Missing document target model, target-aware validation, conversion guidance, and doc-level metadata |
| Actions/code authoring | Actions panel plus Code Snippets panel with template insertion workflows | Freeform frame and behavior scripts exist | Missing snippet library, target-aware starter commands, and insertion workflow |
| Classic paint/style tools | `Eyedropper`, `Paint Bucket`, `Ink Bottle`, rich paint workflow | Style editing exists only in inspector | Missing direct canvas style sampling and stage-applied fill/stroke tools |
| Selection tool family | `Selection`, `Subselection`, `Lasso`, and related object-pick workflows | Selection plus path-point editing already exist | Missing dedicated tool identities and selection-oriented affordances beyond the base select tool |
| Layer parenting | Parent-child transform relationships between layers | Hierarchy/folders exist, but not as transform parenting semantics | Missing explicit parenting workflow and parent-aware transform behavior |
| Asset Warp | Pin/mesh deformation authoring | No warp/deformation authoring model | Missing warp pins, mesh controls, deformation rendering, and editing |
| Modern rigging | Flexi Bone, envelope deformation, pose authoring | No rig/bone model | Missing rig graph, joints, pose interpolation, and rig inspector |
| Document conversion flow | Convert between Animate document targets with guided compatibility handling | Generic file open/save/export only | Missing doc target conversion workflow and compatibility issue reporting |
| Tool-mode affordances | Tool-specific summaries, direct stage feedback, and style pickup/apply behavior | Partial | Missing paint tool stage behaviors and richer tool summaries |

## Priority Assessment

### Highest-value missing features

These are the missing areas that deliver the most practical authoring value with
the least ambiguity:

1. document target semantics and target-aware validation
2. code snippets and target-aware script insertion
3. direct stage paint/style tools

### Largest remaining parity gaps

These are strategically important, but require a new deformation/pose model:

1. layer parenting as transform semantics
2. asset warp
3. modern rigging / Flexi Bone style workflows

## Recommended Bridge Strategy

To close the gap without destabilizing the editor:

### Phase 22. Animate Document Targets

Add a real Animate-like document target model:

- document target type
- ruler/stage units
- target-specific authoring metadata
- target-aware publish validation and conversion messaging

### Phase 23. Style Tools On The Stage

Add direct stage tools:

- `Eyedropper`
- `Paint Bucket`
- `Ink Bottle`
- explicit tool identities for subselection-style point editing

### Phase 24. Actions Snippets

Add a Code Snippets workflow:

- canned commands for common navigation/state workflows
- target-aware starter templates
- one-click insertion into frame or behavior scripts

### Phase 25. Parenting

Add explicit parent-child transform semantics:

- transform inheritance
- parent selection helpers
- parent-aware rendering and hit testing

### Phase 26. Asset Warp And Rigging

Add a deformation layer:

- warp pins and mesh density
- bone/flexi-bone metadata
- pose interpolation
- stage manipulation and preview

## Delivery Standard

Each phase is only complete when it lands in:

- core models
- domain services
- viewmodels
- docked workspace UI
- serialization/persistence
- validation logic
- tests

## Analysis Conclusion

The app is already strong in timeline, symbol, media, and export workflows.
The remaining Animate-specific parity gaps are concentrated in:

- document-target semantics
- direct paint/style tool workflows
- actions/snippet ergonomics
- parenting/warp/rigging

The first three are practical to ship immediately on the existing architecture.
Parenting and deformation need a larger but well-contained follow-up phase.

## Implementation Status

The bridge plan derived from this analysis is now implemented through Phases 22-26:

- Animate document targets and target-aware validation
- direct stage style tools
- actions/code snippets
- layer transform parenting
- asset warp and rigging foundations

The practical remaining Adobe Animate differences are now mostly depth and polish
areas rather than missing first-class features.
