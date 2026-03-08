# Codebase Refactoring Implementation Plan

## Goal

Refactor the solution toward reusable standalone class libraries and smaller
SOLID-aligned files without breaking current product behavior.

This plan is phased so the codebase can keep shipping while being decomposed.

## Refactoring Principles

- preserve behavior first, then extract
- move one feature seam at a time
- split orchestration from domain logic before splitting UI
- keep dependency direction strict
- add regression tests before or during extraction of risky areas

## Phase Overview

| Phase | Scope | Outcome |
| --- | --- | --- |
| 1 | Solution baseline and extraction seams | explicit module boundaries and dependency graph |
| 2 | Split `MainWindowViewModel` into feature-scoped view models | remove god ViewModel |
| 3 | Split shell XAML and reduce `MainWindow.axaml.cs` | passive views and smaller reusable feature views |
| 4 | Extract reusable rendering projects | renderer reusable outside desktop shell |
| 5 | Extract interchange projects | format handlers isolated by format |
| 6 | Split authoring controls into smaller collaborators | maintainable canvas/timeline controls |
| 7 | Theme/resource decomposition | smaller control-theme dictionaries and shell styling files |
| 8 | Final cleanup and dependency enforcement | architecture locked and documented |

## Phase 1: Baseline And Module Preparation

### Tasks

1. Create solution folders and new class-library projects:
   - `TimelineAnimations.Domain`
   - `TimelineAnimations.Engine`
   - `TimelineAnimations.Interop`
   - `TimelineAnimations.Interop.FlashXfl`
   - `TimelineAnimations.Interop.Web`
   - `TimelineAnimations.Rendering`
   - `TimelineAnimations.Rendering.Avalonia`
   - `TimelineAnimations.Application`
2. Move only compile-safe foundational types first:
   - document models
   - timeline enums
   - simple value objects
3. Add explicit project references and remove accidental cross-layer dependencies.

### Deliverables

- updated solution graph
- compile-safe foundational modules
- documented dependency direction

## Phase 2: Split `MainWindowViewModel`

### Tasks

1. Introduce feature-scoped ViewModels:
   - `EditorSessionViewModel`
   - `SceneWorkspaceViewModel`
   - `LibraryWorkspaceViewModel`
   - `MediaWorkspaceViewModel`
   - `InspectorViewModel`
   - `TimelineWorkspaceViewModel`
   - `ToolboxViewModel`
   - `InteropWorkspaceViewModel`
   - `PublishWorkspaceViewModel`
   - `PrototypeWorkspaceViewModel`
2. Keep `MainWindowViewModel` only as a shell coordinator.
3. Move commands/state property-by-property, not by giant file copy.
4. Add tests around migrated feature ViewModels.

### Deliverables

- `MainWindowViewModel` reduced to shell composition
- testable feature-specific ViewModels

## Phase 3: Split Shell XAML And Reduce Code-Behind

### Tasks

1. Break [MainWindow.axaml](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml) into feature views.
2. Replace direct event routing in [MainWindow.axaml.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml.cs) with:
   - behaviors
   - command bindings
   - focused interaction adapters
3. Keep only unavoidable native menu/title-bar bootstrap in code-behind.

### Deliverables

- smaller shell view files
- bootstrap-only window code-behind

## Phase 4: Extract Reusable Rendering

### Tasks

1. Create `TimelineAnimations.Rendering` for:
   - scene composition
   - effect sampling
   - blend mode logic
   - camera transforms
2. Create `TimelineAnimations.Rendering.Avalonia` for:
   - Avalonia `DrawingContext` rendering
   - bitmap/export rendering adapters
3. Split [CompositeFrameRenderer.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs) into:
   - `SceneCompositionService`
   - `BlendModeComposer`
   - `LayerEffectRenderer`
   - `LayerGeometryRenderer`
   - `CameraTransformService`
4. Move reusable helpers out of `App/Helpers` where they are not UI-only.

### Deliverables

- reusable rendering modules
- thinner desktop rendering adapter layer

## Phase 5: Extract Interchange And File Format Modules

### Tasks

1. Create format-handler abstractions in `TimelineAnimations.Interop`.
2. Move [AnimationExchangeService.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/AnimationExchangeService.cs) to a coordinator model.
3. Split [FlashXflExchangeService.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs) into reader/writer/mappers in `TimelineAnimations.Interop.FlashXfl`.
4. Move Avalonia XAML / SVG / HTML handlers into `TimelineAnimations.Interop.Web`.
5. Keep [TimelineAnimations.FlashFormats.csproj](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.FlashFormats/TimelineAnimations.FlashFormats.csproj) as the low-level package/container dependency.

### Deliverables

- reusable per-format interop libraries
- smaller focused format services

## Phase 6: Split Authoring Controls

### Tasks

1. Refactor [SceneCanvasControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/SceneCanvasControl.cs):
   - rendering
   - navigation
   - selection
   - drawing tools
   - deformation
   - prototype
2. Refactor [FrameTimelineControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/FrameTimelineControl.cs):
   - ruler rendering
   - row rendering
   - interaction
3. Refactor [TimelineEditorControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/TimelineEditorControl.cs):
   - ruler rendering
   - hierarchy rendering
   - keyframe rendering
   - interaction

### Deliverables

- smaller controls
- clearer responsibility boundaries

## Phase 7: Theme And UI Resource Decomposition

### Tasks

1. Split [Theme.axaml](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Styles/Theme.axaml) into:
   - `Tokens.axaml`
   - `ControlThemes.axaml`
   - `ShellChrome.axaml`
   - `DockChrome.axaml`
   - `TimelineThemes.axaml`
   - `InspectorThemes.axaml`
2. Keep `App.axaml` as composition only.
3. Move view-local styles into view dictionaries where appropriate.

### Deliverables

- smaller style dictionaries
- simpler theme maintenance

## Phase 8: Final Cleanup And Enforcement

### Tasks

1. Add architecture tests or simple guard checks for dependency direction.
2. Remove dead adapter code after migration.
3. Update documentation and project map.
4. Set size guardrails for future reviews:
   - ViewModels over 1500 lines require justification
   - services over 1000 lines require decomposition review
   - code-behind over 100 lines requires architectural review

### Deliverables

- enforced architecture boundaries
- updated engineering docs

## Detailed First Refactor Order

This is the recommended implementation order to minimize breakage.

1. Create new projects and move pure models/enums first.
2. Extract renderer helpers before touching stage controls.
3. Extract interop helpers before touching UI import/export flows.
4. Split `MainWindowViewModel` by state ownership.
5. Split shell XAML after feature VMs exist.
6. Split large controls after rendering/application services are extracted.
7. Finish by shrinking code-behind and theme files.

## Detailed File-Level Candidates

### Highest priority

- [MainWindowViewModel.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs)
- [MainWindow.axaml](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml)
- [MainWindow.axaml.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml.cs)
- [FlashXflExchangeService.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs)
- [SceneCanvasControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/SceneCanvasControl.cs)
- [AnimationExchangeService.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/AnimationExchangeService.cs)
- [CompositeFrameRenderer.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs)

### Secondary priority

- [FrameTimelineControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/FrameTimelineControl.cs)
- [TimelineEditorControl.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/TimelineEditorControl.cs)
- [Theme.axaml](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Styles/Theme.axaml)
- [DockInspectorToolView.axaml](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/Dock/DockInspectorToolView.axaml)
- [TimelineEditingService.cs](/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/TimelineEditingService.cs)

## Exit Criteria

This plan is complete when:

- reusable editor feature code no longer depends on the desktop shell project
- giant orchestration files are broken into feature-focused types
- code-behind is reduced to minimal bootstrap
- UI views are composed from smaller feature views
- the solution structure communicates feature ownership clearly
