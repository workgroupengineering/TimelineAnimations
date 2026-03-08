# Codebase Refactoring Analysis

## Purpose

This document analyzes the current `TimelineAnimations` codebase and identifies
the refactoring work needed to:

- split reusable feature areas into standalone class libraries
- reduce file size and responsibility overload
- enforce stricter SOLID and MVVM boundaries
- make the codebase easier to test, evolve, and reuse outside the desktop shell

## Current Solution Shape

### Projects

| Project | Current role | Current issue |
| --- | --- | --- |
| `TimelineAnimations.App` | Avalonia desktop shell, controls, rendering, dock UI, view models, app services | Too much feature logic lives here; reusable feature code is mixed with desktop-only UI code |
| `TimelineAnimations.Core` | models, editing services, document workflows, interop logic, publish validation | Too broad; it mixes domain, application logic, and infrastructure/interchange concerns |
| `TimelineAnimations.FlashFormats` | Flash authoring package/container support | Good start, but too narrow; broader interop/infrastructure boundaries are still collapsed into `Core` |

### Current dependency shape

| Layer | Current state | Problem |
| --- | --- | --- |
| UI | `App` | expected |
| Domain/application | mostly `Core` | overloaded |
| Infrastructure | partly `FlashFormats`, partly `Core`, partly `App` | responsibilities are not clearly separated |

## Biggest Structural Hotspots

### Oversized C# files

Measured from the current tree:

| File | Lines | Problem |
| --- | ---: | --- |
| `src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs` | 12400 | God ViewModel; owns too many editor, workspace, interop, publish, tool, and playback responsibilities |
| `src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs` | 3817 | Reader, writer, converters, package semantics, effect mapping, and metadata handling are all mixed together |
| `src/TimelineAnimations.App/Controls/SceneCanvasControl.cs` | 3759 | Rendering, hit testing, tool interaction, pan/zoom, deformation, prototype input, and edit gestures live together |
| `src/TimelineAnimations.Core/Services/AnimationExchangeService.cs` | 2452 | Multi-format import/export and fallback parsing are centralized into one large service |
| `src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs` | 1509 | Scene composition, camera transforms, effect passes, geometry rendering, and blend composition are all in one file |
| `src/TimelineAnimations.App/Views/MainWindow.axaml.cs` | 1256 | Too much code-behind orchestration and event routing for a strict MVVM codebase |
| `src/TimelineAnimations.App/Controls/FrameTimelineControl.cs` | 1074 | Header rendering, body rendering, ruler logic, and interaction are combined |
| `src/TimelineAnimations.App/Controls/TimelineEditorControl.cs` | 1018 | Track rendering, hierarchy, ruler logic, and interaction are combined |
| `src/TimelineAnimations.Core/Services/TimelineEditingService.cs` | 1267 | Editing operations across many feature areas are concentrated into one service |

### Oversized XAML files

| File | Lines | Problem |
| --- | ---: | --- |
| `src/TimelineAnimations.App/Views/MainWindow.axaml` | 6171 | Monolithic shell view with too many inline feature surfaces |
| `src/TimelineAnimations.App/Views/Dock/DockInspectorToolView.axaml` | 2300 | Too many inspector sections in one view |
| `src/TimelineAnimations.App/Styles/Theme.axaml` | 1698 | Global theme, control templates, utility styles, and product-specific shell styles are all mixed |

## Architectural Findings

### 1. `MainWindowViewModel` violates SRP

It currently owns:

- document/session lifecycle
- scene management
- library management
- media management
- inspector state
- timeline state
- curves state
- canvas tool state
- prototype/runtime state
- interop preview/import/export state
- publish workflow state
- dock workspace state
- undo/redo and history
- status/help/menu state

This is the clearest refactoring priority.

### 2. `MainWindow.axaml.cs` is too active for strict MVVM

The window code-behind currently routes many interactions directly to the
ViewModel. Even if those calls are mostly thin, the file is too large and acts
as an orchestration hub. That breaks the target rule that views should stay
passive and code-behind should be limited to initialization and unavoidable
framework glue.

### 3. Interop is not isolated enough

`FlashXflExchangeService` and `AnimationExchangeService` contain reusable
feature logic, but they still live in general-purpose `Core`. They should become
format-specific adapters behind smaller reusable contracts.

### 4. Rendering is reusable but trapped in `App`

`CompositeFrameRenderer`, parts of `SceneCanvasControl`, and helper classes
implement reusable authoring/render semantics, but they are tied to the desktop
application project. The code should be split into:

- reusable scene/render composition logic
- Avalonia-specific drawing adapters
- desktop-only control input surfaces

### 5. App shell and feature workspaces are not separated cleanly

The desktop shell currently owns both:

- application composition
- feature logic

That makes it difficult to reuse editor capabilities in alternate shells,
tools, automation, preview hosts, or test harnesses.

### 6. Theme and view composition are too centralized

Large XAML files indicate the UI is still organized around a few mega-views
instead of feature-scoped reusable views and resource dictionaries.

## Recommended Target Project Layout

This is the recommended end-state project graph.

| Proposed project | Responsibility | Depends on |
| --- | --- | --- |
| `TimelineAnimations.Domain` | pure models, enums, value objects, document graph | none |
| `TimelineAnimations.Engine` | timeline editing, interpolation, scene/media logic, motion presets, deformation, parenting | `Domain` |
| `TimelineAnimations.Interop` | common import/export contracts, diagnostics, format registry abstractions | `Domain`, `Engine` |
| `TimelineAnimations.Interop.FlashXfl` | XFL reader/writer, mapping, package integration | `Interop`, `FlashFormats`, `Domain`, `Engine` |
| `TimelineAnimations.Interop.Web` | Avalonia XAML / SVG / HTML interchange | `Interop`, `Domain`, `Engine` |
| `TimelineAnimations.Rendering` | scene composition pipeline, blend/effect logic, camera transforms, render sampling abstractions | `Domain`, `Engine` |
| `TimelineAnimations.Rendering.Avalonia` | Avalonia drawing implementation, bitmap/export adapters | `Rendering`, `Domain`, `Engine` |
| `TimelineAnimations.Application` | session/document workflows, publish workflows, workspace services, orchestration services | `Domain`, `Engine`, `Interop`, `Rendering` |
| `TimelineAnimations.App` | desktop shell, Avalonia views, controls, dock layout, top-level composition root | all required adapters only |
| `TimelineAnimations.FlashFormats` | low-level authoring package/container IO | none or `Domain` only if needed |

## Recommended File Split Boundaries

### `MainWindowViewModel.cs`

Split into:

| Target type/file | Responsibility |
| --- | --- |
| `EditorSessionViewModel` | document/session lifecycle, recent format, app title, status |
| `SceneWorkspaceViewModel` | scenes, canvas properties, scene markers, transitions |
| `LibraryWorkspaceViewModel` | library items, folders, metadata, component definitions |
| `MediaWorkspaceViewModel` | media assets, import, media placement |
| `InspectorViewModel` | selected layer/object/frame/document property editing |
| `TimelineWorkspaceViewModel` | frame rows, curve rows, keyframe selection, scrubbing, ruler state |
| `ToolboxViewModel` | drawing tool state, tool options, stage quick-strip state |
| `InteropWorkspaceViewModel` | format preview, import/export summaries, code preview |
| `PublishWorkspaceViewModel` | profiles, validation, export actions |
| `PrototypeWorkspaceViewModel` | runtime preview/prototype interactions |
| `DockWorkspaceCoordinatorViewModel` | dock presets, panels, visibility, floating/docking orchestration |

If a single shell VM remains, it should become a very thin coordinator that
composes those sub-viewmodels instead of owning all state directly.

### `MainWindow.axaml`

Split into feature views:

- `TitleBarView`
- `TopCommandBarView`
- `StatusStripView`
- `ClassicProjectPanelView`
- `ClassicInspectorPanelView`
- `ClassicTimelinePanelView`
- `ClassicInteropPanelView`

### `MainWindow.axaml.cs`

Replace direct event hookups with:

- behaviors
- command adapters
- view-level interaction services

At minimum, split into separate helpers for:

- native menu integration
- canvas event routing
- timeline event routing
- window chrome/title-bar integration

### `SceneCanvasControl.cs`

Split into partials or separate collaborators:

| Target | Responsibility |
| --- | --- |
| `SceneCanvas.Rendering` | draw stage, overlays, handles |
| `SceneCanvas.Navigation` | pan, zoom, fit, viewport |
| `SceneCanvas.Selection` | hit testing, lasso, marquee, handles |
| `SceneCanvas.DrawingTools` | rectangle/ellipse/text/pen/brush/polystar creation |
| `SceneCanvas.Deformation` | warp/rig interaction |
| `SceneCanvas.Prototype` | pointer prototype triggers |
| `SceneCanvas.Measurement` | coordinate conversions and transforms |

### `CompositeFrameRenderer.cs`

Split into services:

- `SceneCompositionService`
- `LayerEffectRenderer`
- `LayerGeometryRenderer`
- `BlendModeComposer`
- `CameraTransformService`
- `EffectSamplingService`

### `FlashXflExchangeService.cs`

Split into:

- `FlashXflDocumentReader`
- `FlashXflDocumentWriter`
- `FlashXflTimelineReader`
- `FlashXflTimelineWriter`
- `FlashXflLibraryReader`
- `FlashXflLibraryWriter`
- `FlashXflMediaReader`
- `FlashXflMediaWriter`
- `FlashXflVisualEffectsMapper`
- `FlashXflValueConverter`

### `AnimationExchangeService.cs`

Replace with a format registry:

- `IAnimationExchangeFormatHandler`
- `AvaloniaXamlExchangeHandler`
- `FlashXflExchangeHandler`
- `SvgSmilExchangeHandler`
- `HtmlCssExchangeHandler`
- `AnimationExchangeCoordinator`

## Refactoring Priorities

### Priority 1

- split `MainWindowViewModel`
- reduce `MainWindow.axaml.cs`
- isolate reusable feature services from `App`

### Priority 2

- split interchange services by format
- split renderer into composition/effects/geometry layers
- split giant controls into smaller rendering/input units

### Priority 3

- split giant XAML views into feature views
- split theme into resource dictionaries by concern
- refine DI/composition root to assemble the new module graph

## Success Criteria

The refactor should be considered successful when:

- no single ViewModel exceeds roughly 1000-1500 lines
- no single service exceeds roughly 800-1000 lines without a very strong reason
- code-behind is reduced to bootstrap-only glue
- reusable feature code lives outside `TimelineAnimations.App`
- project references follow a clear one-way architecture
- new feature work can target standalone libraries instead of the desktop shell

## Non-Goals

This refactor plan is not a request to rewrite the product from scratch. The
goal is phased extraction and decomposition while preserving current behavior.
