# Adobe Animate Gap Bridge Plan

## Objective

Close the remaining Adobe Animate authoring gaps in staged slices that can be
implemented, tested, and shipped on the current Avalonia architecture.

## Schedule

| Phase | Scope | Status | Target in this pass |
| --- | --- | --- | --- |
| 22 | Animate document targets and target-aware validation | completed | yes |
| 23 | Direct stage style tools (`Eyedropper`, `Paint Bucket`, `Ink Bottle`) | completed | yes |
| 24 | Actions/code snippets and target-aware insertion | completed | yes |
| 25 | Layer parenting transform semantics | completed | yes |
| 26 | Asset warp and modern rigging foundation | completed | yes |

## Phase 22. Animate Document Targets

### Deliverables

- add a document-level Animate target model
- add document units and authoring metadata
- bind document target into publish profile defaults and validation
- add target conversion command path
- expose the model in the docked inspector `Doc` surface

### Model changes

- add `AnimateDocumentType`
- add `AnimateDocumentUnits`
- add `AnimateDocumentSettings`
- attach settings to `TimelineDocument`

### Service changes

- add `AnimateDocumentProfileService`
- extend `PublishValidationService` with target-aware checks

### UI changes

- doc inspector surface for:
  - target type
  - units
  - responsive/default flags
  - target compatibility summary

### Acceptance

- document target round-trips through serialization
- validation reports target-specific warnings
- user can convert the active document target from the UI

## Phase 23. Direct Stage Style Tools

### Deliverables

- add `Eyedropper`, `PaintBucket`, and `InkBottle` tools
- allow style sampling directly from a layer on the stage
- allow applying fill or stroke style directly by clicking on stage content
- expose tool summaries and active affordances in the workspace

### Model and VM changes

- extend `DrawingTool`
- add sampled-style workflow in the main viewmodel
- add tool-state helpers and status summaries

### Stage changes

- stage hit-testing for style sampling/apply
- style request events from `SceneCanvasControl`
- integrate with existing inspector fill/stroke/gradient state

### Acceptance

- eyedropper samples fill/stroke/gradient from the clicked layer
- paint bucket applies current fill/gradient settings to the clicked layer
- ink bottle applies current stroke settings to the clicked layer

## Phase 24. Actions Snippets

### Deliverables

- add a snippet library for frame actions and behavior scripts
- add target-aware snippet sets for:
  - HTML5 Canvas style document workflows
  - ActionScript-style navigation workflows
- support insertion into the frame script or selected behavior script

### Model and service changes

- add `ActionScriptSnippet`
- add `ActionScriptSnippetCategory`
- add `ActionScriptSnippetService`

### UI changes

- snippet browser in `Motion + Actions`
- insert commands for frame or behavior context
- summary text for the selected snippet

### Acceptance

- users can insert snippets without manually typing commands
- snippet sets react to the active document target

## Phase 25. Layer Parenting

### Deliverables

- explicit transform parent selection
- parent-aware rendering
- parent-aware hit testing and selection overlays
- inspector summary for parent relationships

### Delivered

- transform parent selection and summaries in both shells
- parent-aware world-snapshot sampling across preview and export
- parent-aware stage overlays and motion-path sampling
- Flash XFL round-trip for parenting metadata

## Phase 26. Asset Warp And Modern Rigging

### Deliverables

- warp pins and mesh settings
- rig/bone metadata
- stage authoring handles
- pose interpolation

### Delivered

- warp mesh settings and pin metadata
- rig/bone metadata and saved poses
- stage authoring handles for pins and bones
- deformation-aware path rendering in stage preview and export
- Flash XFL round-trip for warp/rig metadata

## Implementation Order For This Pass

1. Phase 22
2. Phase 23
3. Phase 24
4. Phase 25
5. Phase 26

## Quality Gates

- `dotnet build TimelineAnimations.sln`
- targeted and regression unit tests for:
  - document target round-trip
  - target-aware validation
  - stage style sampling/apply tools
  - snippet insertion behavior
  - transform parenting world sampling
  - warp/rig state authoring
  - Flash XFL parenting/warp/rig round-trip
- smoke launch of the desktop app

## Completion Status

All phases in this bridge plan are now implemented in the current codebase.
