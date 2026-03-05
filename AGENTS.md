# TimelineAnimations Agent Guide

## Goal
- Build a polished Avalonia desktop application for WYSIWYG keyframe animation editing.
- Prioritize functional canvas editing, a timeline editor, playback controls, drag and drop, property inspection, and a distinctive visual design.

## Tech Stack
- .NET 9
- Avalonia UI
- C#
- MVVM with compiled bindings

## Architecture Rules
- Keep UI split into `Views`, `ViewModels`, `Models`, `Services`, and `Controls`.
- Prefer immutable model records for timeline data where practical, and keep editor state in view models/services.
- Use typed `x:DataType` bindings in XAML.
- Keep canvas interaction logic in dedicated controls/services instead of window code-behind.
- Favor composition over code-behind; code-behind should be limited to view initialization glue.

## UX Rules
- The app should feel like a professional motion design tool, not a sample app.
- Use a strong visual system: layered backgrounds, deliberate typography, accent color tokens, and animated affordances.
- Every major editor surface should be usable with both pointer interactions and precise numeric editing.
- Timeline interactions must support selection, dragging, zooming, scrubbing, and keyframe manipulation.

## Quality Bar
- Keep the app buildable with `dotnet build`.
- Keep the main desktop flow runnable with `dotnet run --project src/TimelineAnimations.App`.
- Prefer small, composable classes over monolithic windows/view models.
- Add tests for pure timeline logic where feasible.

## Expected Layout
- `src/TimelineAnimations.Core` for timeline models, interpolation, editing, and serialization
- `src/TimelineAnimations.App` for the desktop app, theme resources, controls, and view models
- `tests/TimelineAnimations.App.Tests` for logic-focused tests
- `docs` for product and implementation plans

## Working Agreement
- Preserve user changes if the repo becomes dirty during work.
- Use `apply_patch` for manual file edits.
- Validate changes with build/tests before closing work.
