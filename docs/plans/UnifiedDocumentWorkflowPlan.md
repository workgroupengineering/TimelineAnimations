# Unified Document Workflow Plan

## Goal

Make the main file workflow support the native project model and the supported interchange formats as first-class document types.

## Current Gap

- Native project JSON supports `Open` and `Save`.
- Avalonia XAML, SVG/SMIL, and HTML/CSS support `Import` and `Export`.
- The app does not yet treat all supported formats uniformly through the main `Open` and `Save` path.

## Scope

This pass will implement:

- unified format detection for:
  - native project JSON
  - Avalonia XAML
  - SVG/SMIL
  - HTML/CSS
- unified load/save service for stream-based document workflows
- `Open` support for all supported document formats
- `Save` support based on the current document format
- current-document format tracking in the editor state
- clearer document format display in the chrome
- regression tests for native and interchange file workflows

## Design

### Format Model

- Add a single enum for the supported persisted document formats.
- Add load/save result models so the UI gets:
  - resolved format
  - loaded document when applicable
  - human-readable summary
  - structured issues

### Core Service

- Add a unified document file service in core that:
  - detects format from filename and content
  - loads from a stream into a `TimelineDocument`
  - saves a `TimelineDocument` to a stream
  - reuses `DocumentSerializer` for the native model
  - reuses `AnimationExchangeService` for interchange formats

### App Wiring

- Track the current document format in `MainWindowViewModel`.
- Update `Open` to accept all supported file types and load through the unified service.
- Update `Save` to save in the current document format through the unified service.
- Keep explicit `Import` / `Export` actions for deliberate interchange operations.

## Validation

- unit tests for format detection
- unit tests for native load/save
- unit tests for interchange load/save
- build, test, and smoke launch
