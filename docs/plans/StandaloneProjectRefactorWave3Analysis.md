# Standalone Project Refactor Wave 3 Analysis

## Scope

This analysis reviews the current solution after the first standalone-project
extraction waves and identifies the next cohesive slice to move out of the app
assembly without breaking MVVM boundaries or introducing circular references.

## Current solution shape

The solution already has these extracted projects:

- `TimelineAnimations.Core`
- `TimelineAnimations.Interop`
- `TimelineAnimations.FlashFormats`
- `TimelineAnimations.Rendering`
- `TimelineAnimations.Rendering.Avalonia`
- `TimelineAnimations.Rendering.SkiaSharp`
- `TimelineAnimations.AppServices`
- `TimelineAnimations.Controls`
- `TimelineAnimations.App`

That first wave removed major domain, interop, rendering, and utility-control
concerns from the app shell, but the app assembly still owns too much
presentation-specific code.

## Current hotspots

Largest remaining files in the app tree at audit time:

| File | Approx. size | Problem |
| --- | ---: | --- |
| `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs` | `13k+` lines | shell orchestrator still owns too many feature slices |
| `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml` | `6k+` lines | shell composition is still large, but mostly view layout rather than reusable logic |
| `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Interop/Services/FlashXflExchangeService.cs` | `4k+` lines | interop depth, but already in the correct standalone assembly |
| `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/SceneCanvasControl.cs` | `4k+` lines | reusable editor surface, but still tightly coupled to app-only types |

The best extraction target is not the shell or stage canvas yet. Those are still
too coupled to `MainWindowViewModel` and app-only orchestration services.

## Why the timeline module is the next best seam

The timeline authoring surfaces are a clean standalone seam because:

- they are reused in both classic and docked shells
- they are already custom controls with stable contracts
- they depend on a small set of presentation row/view-model types
- they do not require `TopLevel`, file dialogs, docking state, or shell-specific services

The reusable timeline module currently consisted of:

- timeline controls:
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/FrameTimelineControl.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/TimelineEditorControl.cs`
- timeline interaction contracts:
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/FrameTimelineInteractionEventArgs.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/TimelineEditorInteractionEventArgs.cs`
- presentation row/view-model types:
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/LayerFrameTimelineRowViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/TimelineTrackRowViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/PropertyTrackViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/FrameSpanViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/KeyframeViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/FrameLabelViewModel.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/SceneMarkerViewModel.cs`
- supporting presentation types:
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/ViewModelBase.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Models/FrameTimelineRowKind.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Models/TimelineTrackRowKind.cs`
  - `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Models/TimelineTrackGroupKind.cs`

This is enough code to justify a dedicated presentation project while still
remaining small enough to extract safely in one refactor pass.

## Constraints

- preserve existing CLR namespaces so XAML and consuming code do not require broad rewrites
- avoid introducing any dependency from standalone projects back into `TimelineAnimations.App`
- keep `SceneCanvasControl` in the app for now because it still depends on app-only types such as `LayerViewModel`
- do not split the shell or dock workspace until the host abstraction is narrower

## Recommended extraction

### New standalone project

Create `TimelineAnimations.Presentation` for reusable presentation-layer types:

- base view-model class
- timeline/editor view models
- lightweight presentation enums/models
- presentation helpers such as color conversion

### Expanded controls project responsibility

Move timeline controls from `TimelineAnimations.App` into `TimelineAnimations.Controls`
so the controls project owns actual reusable editor surfaces rather than only
small utility widgets.

## Expected outcome

After this wave:

- app assembly owns less reusable authoring logic
- controls assembly contains the reusable timeline surfaces
- presentation assembly contains reusable timeline row/view-model contracts
- future stage-canvas extraction has a cleaner precedent and fewer dependencies

## Remaining next-wave candidates after this pass

- `SceneCanvasControl` plus its supporting interaction args and stage-only presentation types
- dock workspace host/factory once host abstractions are narrowed
- shell command/menu orchestration once `MainWindowViewModel` is decomposed further
