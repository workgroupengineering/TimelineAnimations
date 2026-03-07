# Animation Format Preview Plan

## Goal

Add a professional preview workspace for supported animation interchange formats so the app can inspect exported code and visually validate the result before saving or sharing it.

## Current Gap

- The app supports import/export and unified open/save for:
  - native project JSON
  - Avalonia XAML
  - SVG / SMIL
  - HTML / CSS
- The `Output` tab only shows a format selector, summary text, and the last import/export diagnostics.
- There is no editor-grade code surface for generated interchange content.
- There is no live preview surface for exported interchange formats.
- Avalonia XAML export is not currently validated through the runtime XAML loader.

## Scope

This pass will implement:

- an interop preview workspace in the `Output` tab
- a read-only code surface using AvaloniaEdit
- syntax highlighting for XAML, SVG/XML, and HTML/CSS
- a refreshable preview pipeline based on the current document snapshot
- runtime XAML preview for Avalonia XAML using the Avalonia runtime XAML loader
- visual preview for SVG / HTML through safe renderable preview snapshots
- structured preview diagnostics separated from the last import/export operation
- preview mode switching:
  - code
  - preview
  - split
- tests for preview generation and viewmodel state

## Design

### Preview State

- Track preview state separately from import/export history.
- The preview state must contain:
  - selected format
  - generated code
  - preview summary
  - preview diagnostics
  - a renderable preview document when available
  - preview mode (`Code`, `Preview`, `Split`)

### Code Surface

- Use AvaloniaEdit `TextEditor` as the code viewer.
- Keep the editor read-only.
- Apply TextMate grammars based on the selected animation format:
  - Avalonia XAML -> XML / XAML grammar
  - SVG / SMIL -> XML grammar
  - HTML / CSS -> HTML grammar

### Visual Preview

- Avalonia XAML:
  - load the generated XAML through `AvaloniaRuntimeXamlLoader`
  - host the loaded control inside a preview container
  - surface loader failures as preview diagnostics
- SVG / HTML:
  - export the current document
  - import the generated format back through the existing fallback importer
  - render the imported document snapshot into a bitmap preview
  - use the same renderer path as publish/frame preview so the result is deterministic

### UI

- Replace the current interop summary block with a denser workspace:
  - toolbar with format selector and refresh/import/export actions
  - compact summary strip
  - preview mode tabs
  - code/preview surface below
- Keep the last import/export result visible, but move it into a smaller diagnostics section.

## Validation

- unit tests for preview generation across supported formats
- unit tests for viewmodel preview-mode and refresh state
- build, test, and smoke launch
