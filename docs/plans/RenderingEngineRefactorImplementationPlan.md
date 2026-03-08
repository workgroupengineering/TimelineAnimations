# Rendering Engine Refactor Implementation Plan

## Objective

Deliver a pluggable rendering architecture with:

- reusable rendering services outside UI controls
- Avalonia renderer backend
- SkiaSharp renderer backend
- user-selectable renderer preference
- full app integration for stage, export, and preview

## Phase 1: Shared Rendering Contracts

Status: `completed`

### Tasks

- Add `TimelineAnimations.Rendering` project.
- Introduce:
  - `RenderingEngineKind`
  - `SceneRenderRequest`
  - `RenderedFrame`
  - `ISceneRenderEngine`
  - `SceneRenderRequestBuilder`
  - `SceneRenderTransformService`
- Move renderer-neutral camera/document transform logic out of
  `CompositeFrameRenderer`.
- Keep the shared project free of Avalonia UI control dependencies.

### Deliverables

- reusable scene render request model
- reusable pixel-frame result contract
- engine abstraction for app integration

## Phase 2: Avalonia Backend Extraction

Status: `completed`

### Tasks

- Add `TimelineAnimations.Rendering.Avalonia` project.
- Port the current compositor into `AvaloniaSceneRenderEngine`.
- Move Avalonia-specific style/vector/media helper code out of the app project
  where it belongs to rendering.
- Preserve all current rendering features.

### Deliverables

- `AvaloniaSceneRenderEngine`
- no direct control dependency in rendering backend

## Phase 3: SkiaSharp Backend

Status: `completed`

### Tasks

- Add `TimelineAnimations.Rendering.SkiaSharp` project.
- Add `SkiaSharp` package references.
- Implement `SkiaSceneRenderEngine` with feature parity:
  - fills/strokes/gradients
  - text
  - vector geometry
  - masks
  - blend modes
  - color effects and filters
  - media layers
  - Avalonia-control visual placeholders
- Return `RenderedFrame` pixel output matching the shared contract.

### Deliverables

- second pluggable renderer engine
- renderer-neutral output for export/preview usage

## Phase 4: App Integration

Status: `completed`

### Tasks

- Add app-level rendering facade and engine registry.
- Add persisted renderer preference service.
- Expose renderer selection in `MainWindowViewModel`.
- Add renderer selection UI in document/settings surfaces.
- Route:
  - stage rendering
  - frame export
  - monitor preview
  - publish preview
  - interop visual preview
  through the facade.

### Deliverables

- selectable render engine in the app
- persisted user preference
- unified rendering entry point for all consumers

## Phase 5: Validation

Status: `completed`

### Tasks

- Add focused tests for:
  - request building
  - transform math
  - Avalonia engine frame generation
  - SkiaSharp engine frame generation
  - preference persistence
  - app selection flow
- Run build and targeted test validation.

### Deliverables

- regression coverage for the new rendering architecture
- validated build using both backends

## Validation

Status: `completed`

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- focused tests:
  - `RendererPreferencesPersistenceService_RoundTrips_SelectedEngine`
  - `MainWindowViewModel_Updates_RenderingEngineHost_WhenSelectionChanges`
  - `RenderingEngineHostService_Renders_With_Avalonia_And_Skia_Engines`
  - `AnimationExchangeVisualPreviewControl_Loads_RuntimeAvaloniaPreview`
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`

## Notes

- The new rendering architecture is complete and pluggable.
- The SkiaSharp backend now renders common authored content natively:
  background, rectangles, ellipses, text, vector paths, and Avalonia-control
  placeholder visuals.
- Unsupported or higher-complexity samples such as effect-heavy layers,
  deformed geometry, and media-backed video still use per-sample Avalonia
  fallback so the engine remains selectable without reducing supported output.

## Scheduling

### Sequence

1. Shared contracts
2. Avalonia extraction
3. SkiaSharp backend
4. App integration
5. Validation

### Execution rule

Do not wire selection UI before both engines can render the same shared request
shape. Avoid partial integration that leaves export, preview, and stage on
different renderer paths.
