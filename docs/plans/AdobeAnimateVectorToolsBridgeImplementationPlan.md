# Adobe Animate Vector Tools Bridge Implementation Plan

Status: completed
Owner: Codex
Date: 2026-03-08

## Objective

Implement the remaining Adobe Animate-style vector tool behavior gaps across
core services, stage interaction, tool state, and both editor shells.

## Schedule

| Phase | Scope | Duration | Status |
| --- | --- | --- | --- |
| 48 | Width profiles and tool-state models | 0.5 day | completed |
| 49 | Stage Width tool interaction | 0.5 day | completed |
| 50 | Gap-aware Paint Bucket and Lock Fill | 0.5 day | completed |
| 51 | Docked/classic UI parity and regression coverage | 0.5 day | completed |

## Phase 48: Width profiles and tool-state models

### Tasks

- add editor-facing enums for width profiles and Paint Bucket gap size
- add a standalone service for applying reusable width profiles
- add localized width-adjustment logic for stage-driven width edits
- use width profiles when generating new brush, pen, line, and pencil paths

## Phase 49: Stage Width tool interaction

### Tasks

- add a dedicated Width tool to the tool model, menus, and rails
- add canvas interaction for drag-based stroke width adjustment
- keep selected path point state synchronized with Width tool interaction
- expose width-profile application for the selected path

## Phase 50: Gap-aware Paint Bucket and Lock Fill

### Tasks

- add a standalone Paint Bucket decision service
- compute whether an open path gap is small enough to fill
- expose `None`, `Small`, `Medium`, and `Large` gap handling
- expose Lock Fill and preserve existing gradient transforms when enabled

## Phase 51: Docked/classic parity and tests

### Tasks

- surface Width tool, width profiles, Paint Bucket gap size, and Lock Fill in
  docked stage, docked inspector, and classic shell controls
- update tool summaries and tool state properties
- add focused unit and headless regressions
- validate with build, focused tests, and app smoke run

## Validation

Planned validation for this slice:

- `dotnet build`
- focused unit tests for width profiles, Paint Bucket decisions, and ViewModel integration
- desktop smoke run
