# Standalone Project Extraction Analysis

Status: completed
Owner: Codex
Date: 2026-03-08

## Goal

Refactor the current solution so reusable controls, reusable application
services, and file-format/interchange workflows live in standalone projects
instead of remaining bundled inside the desktop shell or overloaded `Core`.

## Current structure

The solution already has partial modularization:

- `TimelineAnimations.Core`
- `TimelineAnimations.FlashFormats`
- `TimelineAnimations.Rendering`
- `TimelineAnimations.Rendering.Avalonia`
- `TimelineAnimations.Rendering.SkiaSharp`
- `TimelineAnimations.App`

The main remaining structural issue is that `TimelineAnimations.App` still owns:

- generic reusable Avalonia controls
- reusable UI helpers
- reusable export/render/preview/persistence services

and `TimelineAnimations.Core` still owns:

- interchange contracts
- animation exchange services
- document file workflow services
- Flash XFL authoring interchange services

## Extraction findings

### 1. Generic controls are still trapped in the shell project

The following controls are reusable and do not need to stay in the desktop
shell assembly:

- `AnimationExchangeCodeEditorControl`
- `AnimationExchangeVisualPreviewControl`
- `ColorPropertyEditorControl`
- `EasingCurveEditorControl`
- `TitleBarDragGrid`

Their support helpers are also reusable:

- `ColorHelpers`
- `FluentIconData`
- `TitleBarDragHelper`

### 2. Reusable application services are still inside `TimelineAnimations.App`

The following services are application-level reusable logic and are not tied to
the `MainWindow` shell:

- `AnimationExchangePreviewService`
- `CompositeFrameRenderer`
- `EditorMonitorPreviewService`
- `FrameExportService`
- `MediaAssetRenderService`
- `MediaImportService`
- `PublishExportService`
- `RenderedFrameBitmapFactory`
- `RendererPreferencesPersistenceService`
- `RenderingEngineHostService`
- `WorkspaceLayoutPersistenceService`

These services also depend on a small set of reusable app models:

- `AnimationExchangePreviewMode`
- `AnimationExchangePreviewResult`
- `RendererPreferencesState`
- `WorkspaceLayoutPreset`
- `WorkspaceLayoutState`
- `WorkspacePanelMode`

### 3. File-format workflows should no longer live in `Core`

The following types are isolated enough to extract into a dedicated interop
project without creating a dependency cycle:

- `AnimationExchangeService`
- `FlashXflExchangeService`
- `TimelineDocumentFileService`
- `AnimationExchange*` models
- `TimelineDocumentFile*` models
- `FlashDocumentConversionResult`

These files depend on `Core` models/services and `FlashFormats`, but the rest of
`Core` does not depend on them, so they can move out cleanly.

## Recommended project split

| New project | Responsibility | Depends on |
| --- | --- | --- |
| `TimelineAnimations.Controls` | reusable Avalonia controls and UI helpers | `Core`, `Interop`, `AppServices`, `Rendering` |
| `TimelineAnimations.AppServices` | reusable export, preview, persistence, and render-host services | `Core`, `Interop`, `Rendering*`, `Avalonia` |
| `TimelineAnimations.Interop` | animation exchange, document file workflows, XFL interchange | `Core`, `FlashFormats` |

## Extraction strategy

1. Extract `Interop` first, because it has the cleanest seam and no UI
   dependency.
2. Extract `AppServices` second, moving reusable models with it to avoid
   project cycles.
3. Extract `Controls` last, referencing the new reusable service and interop
   projects.

## Risks

- namespace churn would create too much noise, so the extraction should preserve
  existing namespaces while changing only project ownership
- `TimelineAnimations.App` views must keep compiling against moved controls and
  helpers
- tests that referenced old project ownership must keep building through
  transitive project references

## Success criteria

- `TimelineAnimations.App` no longer directly owns generic controls, generic UI
  helpers, reusable export/preview/persistence services, or file-format
  workflows
- standalone project references express feature ownership clearly
- the solution still builds and focused tests still pass

## Outcome

The extraction was implemented with three new projects:

- `TimelineAnimations.Interop`
- `TimelineAnimations.AppServices`
- `TimelineAnimations.Controls`

Namespaces were preserved to minimize churn while ownership moved to dedicated
assemblies. The solution builds successfully, and focused validation covering
interop, reusable services, and extracted controls passes.
