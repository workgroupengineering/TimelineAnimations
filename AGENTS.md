# TimelineAnimations Agent Guide

## Goal
- Build a polished Avalonia desktop application for WYSIWYG keyframe animation editing.
- Prioritize functional canvas editing, a timeline editor, playback controls, drag and drop, property inspection, and a distinctive visual design.

## Tech Stack
- .NET 9
- Avalonia UI
- C#
- MVVM with compiled bindings

## Non-Negotiable Principles

### SOLID (strict)
- Single Responsibility: every class, service, view model, and control must have one clear reason to change.
- Open/Closed: extend behavior through focused abstractions and composition; do not keep patching stable classes with unrelated responsibilities.
- Liskov Substitution: derived or specialized types must preserve the contract of the abstraction they implement.
- Interface Segregation: prefer small focused interfaces over broad service contracts.
- Dependency Inversion: depend on abstractions at boundaries and wire concrete implementations in the composition root only.

### MVVM (strict)
- Views are passive. No business logic, workflow orchestration, or state mutation in code-behind.
- Code-behind is limited to `InitializeComponent()` and unavoidable Avalonia view bootstrap only.
- All user interactions must flow through bindings, commands, behaviors, or control abstractions that keep the ViewModel testable.
- ViewModels must stay framework-agnostic wherever practical and must not depend on view types.
- Models and services own domain logic; ViewModels coordinate them and expose state to the UI.
- Do not move logic into custom controls when it belongs in services or ViewModels; controls should focus on reusable presentation and interaction surfaces.

## Architecture Rules
- Keep UI split into `Views`, `ViewModels`, `Models`, `Services`, and `Controls`.
- Keep dependencies flowing one way: `Views -> ViewModels -> Services/Models`.
- Do not reference Avalonia UI types from domain/service code in `src/TimelineAnimations.Core`.
- Prefer immutable model records for timeline data where practical, and keep editor state in view models/services.
- Use typed `x:DataType` bindings in XAML.
- Keep canvas interaction logic in dedicated controls/services instead of window code-behind.
- Favor composition over code-behind; code-behind should be limited to view initialization glue.
- Prefer decomposition over large multi-purpose view models; split by feature area when responsibilities grow.

## UX Rules
- The app should feel like a professional motion design tool, not a sample app.
- Use a strong visual system: layered backgrounds, deliberate typography, accent color tokens, and animated affordances.
- Every major editor surface should be usable with both pointer interactions and precise numeric editing.
- Timeline interactions must support selection, dragging, zooming, scrubbing, and keyframe manipulation.
- Use Microsoft Fluent System Icons for product UI chrome, toolbars, tool rails, and panel commands where an icon meaning is clear.
- Source Fluent icon path data from the official Microsoft Fluent System Icons set and keep it in reusable named constants/resources; do not hand-draw SVG path data.
- Prefer icon-only controls for Adobe Animate-style tool rails and icon-plus-text controls for command bars, panel actions, and document-level commands.
- All actionable buttons and toggle buttons must have a tooltip. Text buttons may use a shared fallback tooltip, but icon-only buttons must always have an explicit descriptive tooltip.

## Quality Bar
- Keep the app buildable with `dotnet build`.
- Keep the main desktop flow runnable with `dotnet run --project src/TimelineAnimations.App`.
- Prefer small, composable classes over monolithic windows/view models.
- Add tests for pure timeline logic where feasible.
- Reject changes that weaken MVVM boundaries or introduce non-SOLID shortcuts unless there is an explicit documented architectural reason.

## Expected Layout
- `src/TimelineAnimations.Core` for timeline models, interpolation, editing, and serialization
- `src/TimelineAnimations.App` for the desktop app, theme resources, controls, and view models
- `tests/TimelineAnimations.App.Tests` for logic-focused tests
- `docs` for product and implementation plans

## Working Agreement
- Preserve user changes if the repo becomes dirty during work.
- Use `apply_patch` for manual file edits.
- Validate changes with build/tests before closing work.
