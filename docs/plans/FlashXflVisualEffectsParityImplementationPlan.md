# Flash XFL Visual Effects Parity Implementation Plan

## Status

Completed on March 7, 2026.

## Phase 1: Core model expansion

- Add a `FlashColorEffectMode` model.
- Extend `LayerCompositeSettings` with explicit Flash color-effect fields:
  - alpha percent
  - tint color / tint percent
  - brightness percent
  - advanced RGBA multipliers and offsets
- Keep defaults neutral so existing documents do not change behavior.

## Phase 2: Native XFL import/export mapping

- Export native Flash color-effect attributes on visual elements.
- Export native Flash filter collections using:
  - `BlurFilter`
  - `GlowFilter`
  - `DropShadowFilter`
- Import the same attributes/elements from external XFL and map them into the layer compositing model.

## Phase 3: Rendering parity

- Centralize color-effect application in a shared helper.
- Apply the shared logic in:
  - stage preview
  - onion-skin preview
  - export renderer
- Keep filter rendering aligned with the existing blur / glow / shadow effect passes.

## Phase 4: Inspector and editing parity

- Add a Flash-oriented color effect section in the inspector.
- Surface mode-specific controls for:
  - alpha
  - tint
  - brightness
  - advanced channel transforms
- Keep summaries and selection refresh aligned with imported XFL state.

## Phase 5: Validation

- Add regression tests for:
  - native XFL color-effect round-trip
  - native XFL filter round-trip
  - renderer color-effect output math
  - viewmodel inspector refresh for Flash color effects
- Run build, tests, and a smoke launch.

## Result

Implemented in:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/FlashColorEffectMode.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerCompositeSettings.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Helpers/ColorHelpers.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/SceneCanvasControl.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineCoreTests.cs`

Verification completed with:

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`

## Acceptance criteria

- Adobe-authored XFL color effects survive import/export without flattening into generic metadata only.
- Native XFL blur / glow / drop-shadow filters round-trip through the app.
- Stage preview and exported output show the same Flash color-effect result.
- The inspector can edit the Flash-specific color-effect surface directly.
