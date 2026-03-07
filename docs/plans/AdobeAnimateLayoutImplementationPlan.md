# Adobe Animate Layout Implementation Plan

Status: completed on March 6, 2026.

Companion analysis:

- [AdobeAnimateLayoutAnalysis.md](/Users/wieslawsoltes/GitHub/TimelineAnimations/docs/plans/AdobeAnimateLayoutAnalysis.md)

## Implementation Scope

This pass focuses on the parts of the application that define the day-to-day authoring
experience:

- Dock workspace frame
- left tool rail
- navigator panel
- right inspector panel
- output/support panel
- top workspace strip

## Phase 1. Reframe The Dock Workspace As An Animate-Style Shell

Files:

- `src/TimelineAnimations.App/Views/Dock/DockWorkspaceHostView.axaml`
- `src/TimelineAnimations.App/ViewModels/Dock/DockWorkspaceHostViewModel.cs`
- `src/TimelineAnimations.App/Styles/Theme.axaml`

Deliver:

- replace the generic dock organizer bar with:
  - workspace preset tabs
  - document/context summary
  - grouped panel menus
- add a fixed left tools rail inside the dock workspace frame
- keep DockControl as the movable panel surface

Acceptance:

- the dock workspace visibly resembles an animation authoring desk
- tools remain fixed while dock panels move

## Phase 2. Retab The Navigator Panel

Files:

- `src/TimelineAnimations.App/Views/Dock/DockProjectToolView.axaml`

Deliver:

- replace long stacked sections with tabs:
  - `Scenes`
  - `Layers`
  - `Library`
  - `Media`

Acceptance:

- left navigation is compact and scan-friendly
- library and media no longer require long vertical travel

## Phase 3. Retab The Inspector Around Animate Concepts

Files:

- `src/TimelineAnimations.App/Views/Dock/DockInspectorToolView.axaml`

Deliver:

- introduce:
  - `Tool`
  - `Object`
  - `Frame`
  - `Doc`
- map existing editors into those tabs

Acceptance:

- object transforms/styles live in `Object`
- frame labels/markers/actions live in `Frame`
- canvas/document settings live in `Doc`
- current tool and stage behavior live in `Tool`

## Phase 4. Group Output And Support Workflows

Files:

- `src/TimelineAnimations.App/Views/Dock/DockOutputToolView.axaml`

Deliver:

- retab the output surface into:
  - `Publish`
  - `Interop`
  - `Preview`

Acceptance:

- export/publish/interchange are easier to scan
- support workflows feel like grouped studio panels, not a long sheet

## Phase 5. Align The Top Workspace Strip

Files:

- `src/TimelineAnimations.App/Views/MainWindow.axaml`

Deliver:

- when the dock workspace is active, show Animate-style workspace preset tabs
- reduce the visible “layout management” noise in the top shell
- keep direct access to save/export/prototype flows

Acceptance:

- top shell feels closer to Animate workspaces
- everyday controls are visible; low-frequency layout mechanics move into panel menus

## Phase 6. Validation

Files:

- `tests/TimelineAnimations.App.Tests/TimelineCoreTests.cs`
- `tests/TimelineAnimations.App.Tests/TimelineHeadlessTests.cs`

Deliver:

- keep dock preset and surface-management tests green
- confirm headless render still works
- smoke-run the app

Acceptance:

- solution builds
- tests pass
- runtime shell launches successfully

Result:

- `dotnet build TimelineAnimations.sln` passed
- `dotnet test TimelineAnimations.sln --no-build` passed with 90/90 tests
- `dotnet run --project src/TimelineAnimations.App --no-build` smoke-launched successfully
