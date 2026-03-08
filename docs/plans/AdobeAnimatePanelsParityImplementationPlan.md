# Adobe Animate Panels Parity Implementation Plan

## Objective

Bridge the remaining panel-parity gap with Adobe Animate by promoting existing authoring features into dedicated dock panels and adding the missing shared state for Swatches, History, and Movie Explorer.

## Schedule

| Phase | Scope | Status |
| --- | --- | --- |
| 44 | Shared panel state for swatches, history, and movie explorer | planned |
| 45 | New dock panel surfaces and views | planned |
| 46 | Dock layout, panel menus, preset integration, and surface metadata | planned |
| 47 | Focused validation and tests | planned |

## Phase 44: Shared panel state

### Tasks

- add reusable swatch models and preset palette data
- add swatch-application commands for fill, stroke, gradients, and scene backgrounds
- add a recent-operations history journal
- add movie-explorer node models, filtering, summaries, and focus commands

### Target files

- `src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs`
- `src/TimelineAnimations.App/ViewModels/*.cs`

## Phase 45: New dock surfaces

### Panels to add

- `Library`
- `Color`
- `Swatches`
- `Align`
- `Transform`
- `Info`
- `Motion Presets`
- `Code Snippets`
- `Components`
- `History`
- `Movie Explorer`

### Tasks

- add dock tool viewmodels for each panel
- add dock views for each panel using compiled bindings
- reuse current `MainWindowViewModel` commands and editors instead of cloning logic

### Target files

- `src/TimelineAnimations.App/ViewModels/Dock/*`
- `src/TimelineAnimations.App/Views/Dock/*`

## Phase 46: Dock integration

### Tasks

- add new surface IDs and labels
- expose panel labels in `DockWorkspaceHostViewModel`
- update dock factory presets to include the new surfaces in Animate-like groups
- extend panel menus in `MainWindow.axaml`
- ensure all new surfaces participate in focus / hide / overlay / reveal flows

### Target files

- `src/TimelineAnimations.App/ViewModels/Dock/DockWorkspaceSurfaceIds.cs`
- `src/TimelineAnimations.App/ViewModels/Dock/DockWorkspaceHostViewModel.cs`
- `src/TimelineAnimations.App/ViewModels/Dock/DockWorkspaceFactory.cs`
- `src/TimelineAnimations.App/Views/MainWindow.axaml`

## Phase 47: Validation

### Tasks

- add focused core tests for swatches, history, and movie explorer behavior
- add headless dock render tests for the new panels
- run build and targeted test coverage

### Target files

- `tests/TimelineAnimations.App.Tests/TimelineCoreTests.cs`
- `tests/TimelineAnimations.App.Tests/TimelineHeadlessTests.cs`

## Completion criteria

- every major Adobe Animate authoring panel has a corresponding dock surface
- new surfaces are reachable from the dock panel menus
- new surfaces can be focused, hidden, overlayed, and floated
- history and movie explorer are no longer missing from the dock workspace
- swatches provide a direct reusable color workflow
