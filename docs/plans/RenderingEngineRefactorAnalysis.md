# Rendering Engine Refactor Analysis

## Goal

Refactor rendering into reusable standalone services that are not coupled to UI
controls, make the renderer pluggable, add a second SkiaSharp-based engine, and
allow users to choose the active engine from the editor.

## Current State

The current rendering pipeline is concentrated in the app layer and is tightly
coupled to Avalonia drawing primitives.

### Main coupling points

- `src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs`
  - mixes scene-request building, transform math, compositing, filters, media
    drawing, text drawing, path drawing, Avalonia-control drawing, and bitmap
    creation in one static service
  - depends directly on `DrawingContext`, `WriteableBitmap`, Avalonia brushes,
    pens, and geometry types
- `src/TimelineAnimations.App/Services/FrameExportService.cs`
  - calls `CompositeFrameRenderer.BuildSceneState(...)` and
    `CompositeFrameRenderer.RenderBitmap(...)` directly
- `src/TimelineAnimations.App/Services/EditorMonitorPreviewService.cs`
  - depends on `FrameExportService.RenderFrameBitmap(...)`
- `src/TimelineAnimations.App/ViewModels/PreviewPlayerViewModel.cs`
  - depends on `FrameExportService.RenderFrameBitmap(...)`
- `src/TimelineAnimations.App/Controls/AnimationExchangeVisualPreviewControl.axaml.cs`
  - depends on `FrameExportService.RenderFrameBitmap(...)`
- `src/TimelineAnimations.App/Controls/SceneCanvasControl.cs`
  - renders the stage through `CompositeFrameRenderer`
  - duplicates camera/state/transform logic that should be reusable and
    renderer-independent

### Reusable logic already available

- `src/TimelineAnimations.Core/Services/SymbolRenderService.cs`
  - builds `RenderableLayerSample` data without UI dependencies
- `src/TimelineAnimations.Core/Models/RenderableLayerSample.cs`
- `src/TimelineAnimations.Core/Models/LayerSnapshot.cs`

These provide a strong foundation for a reusable render-request layer.

## Problems

### Architectural

- Rendering is trapped inside the app project.
- UI controls know too much about renderer internals.
- There is no stable rendering abstraction for export, previews, monitors, and
  stage rendering.
- The renderer cannot be swapped without touching many app-layer call sites.

### Reuse

- Export, preview, monitor, and stage rendering all share the same logic but
  access it through app-specific services.
- There is no standalone rendering project that other tools or services could
  consume.

### Extensibility

- No engine registration model exists.
- No user-facing renderer preference exists.
- Adding SkiaSharp today would require copy/paste directly into app services.

### Maintainability

- `CompositeFrameRenderer.cs` is a large monolith.
- Camera/document transforms are mixed with rasterization.
- Media, effects, vector geometry, and blend logic are not separated by
  responsibility.

## Refactor Target

Introduce a three-layer rendering structure.

### 1. Shared rendering contracts

New project:

- `src/TimelineAnimations.Rendering`

Responsibilities:

- renderer-neutral request model
- renderer-neutral frame result model
- renderer selection enum
- engine interface
- shared scene-request builder
- shared transform and camera math

### 2. Avalonia renderer backend

New project:

- `src/TimelineAnimations.Rendering.Avalonia`

Responsibilities:

- implement the current full rendering feature set using Avalonia primitives
- port the existing compositor out of the app project
- keep rendering isolated from controls and windows

### 3. SkiaSharp renderer backend

New project:

- `src/TimelineAnimations.Rendering.SkiaSharp`

Responsibilities:

- implement the same renderer interface using `SkiaSharp`
- support all scene features currently handled by the Avalonia compositor
- return a renderer-neutral pixel frame

## Required Feature Coverage

The new rendering abstraction must preserve support for the current feature set:

- scenes, scene playback, scene transitions
- camera transforms and parallax depth
- 2D and 3D document transforms
- masks and layer roles
- blend modes
- Flash-style color effects
- blur, glow, shadow, bevel, gradient glow, gradient bevel
- text and Flash text-field variants
- vector paths, Bezier handles, variable width, gradients, primitive shapes
- image/video placeholders and video frame rendering
- Avalonia control layer rendering
- guide/camera exclusions and export-visible scene ordering

## User-Facing Outcome

After the refactor:

- the stage will render through an app-level rendering facade
- export and preview paths will use the same facade
- users will be able to choose `Avalonia` or `SkiaSharp`
- the selected renderer will persist in user settings
- rendering code will be reusable outside controls

## Risks

### SkiaSharp feature fidelity

The Avalonia renderer already contains many product-specific features. SkiaSharp
must match them closely enough for editor preview and export parity.

### Existing dirty worktree

The repo already has many unrelated changes. Refactor work must avoid reverting
or disturbing unrelated in-progress files.

### Static call sites

Many current code paths assume static renderer/export services. Introducing a
pluggable facade must minimize churn while still moving responsibility out of
controls.

## Recommended Execution Order

1. Add plan docs and new rendering projects.
2. Extract shared models and request/transform services.
3. Move the current Avalonia compositor behind an engine interface.
4. Implement the SkiaSharp backend.
5. Add an app-level rendering facade and persisted engine settings.
6. Rewire stage, preview, and export code paths to the facade.
7. Add focused rendering-engine tests and build validation.
