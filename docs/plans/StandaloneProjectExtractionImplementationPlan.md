# Standalone Project Extraction Implementation Plan

Status: completed
Owner: Codex
Date: 2026-03-08

## Objective

Extract reusable controls, reusable application services, and file-format
workflows into dedicated standalone projects while preserving namespaces and
current behavior.

## Schedule

| Phase | Scope | Status |
| --- | --- | --- |
| 1 | Create `TimelineAnimations.Interop` and move file-format services/models | completed |
| 2 | Create `TimelineAnimations.AppServices` and move reusable services/models | completed |
| 3 | Create `TimelineAnimations.Controls` and move generic controls/helpers | completed |
| 4 | Update solution references, validate build, and run focused tests | completed |

## Phase 1: Interop extraction

### Tasks

- create `TimelineAnimations.Interop.csproj`
- move animation exchange models
- move document file models
- move `AnimationExchangeService`
- move `FlashXflExchangeService`
- move `TimelineDocumentFileService`

## Phase 2: App service extraction

### Tasks

- create `TimelineAnimations.AppServices.csproj`
- move reusable preview/export/render/persistence services
- move the small app models those services depend on
- keep shell-only services in `TimelineAnimations.App`

## Phase 3: Controls extraction

### Tasks

- create `TimelineAnimations.Controls.csproj`
- move generic controls and generic UI helpers
- keep highly app-coupled authoring controls in `TimelineAnimations.App`

## Phase 4: Validation

### Tasks

- add the new projects to the solution
- update project references
- build the full solution
- run focused tests that cover interop, reusable services, and moved controls

## Validation result

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
  passed
- focused extraction validation passed:
  `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineAnimations.App.Tests.csproj --no-build --filter "TimelineDocumentFileService|AnimationExchangePreviewService|RendererPreferencesPersistenceService|WorkspaceLayoutPersistenceService|DockOutputToolView_Renders_Interop_And_Publish_Workspace|AnimationExchangeVisualPreviewControl_"`
- result: `25/25` passing

## Notes

- Existing namespaces were preserved deliberately so public XAML and C# usage
  stayed stable while the code moved to dedicated assemblies.
- A full `dotnet test` run still exposes older unrelated headless font failures
  and an existing stack-overflow path outside this refactor. Those are not
  introduced by the standalone project extraction.
