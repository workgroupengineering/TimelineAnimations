# Flash XFL Implementation Plan

## Phase 1: Format surface

- Add `FlashXfl` to the animation exchange format enum.
- Add `FlashXfl` to the unified document file format enum.
- Extend format labels, suggested extensions, and format detection.
- Add `.xfl` support everywhere and `.fla` load detection where practical.

## Phase 2: Core XFL interchange service

- Create a dedicated `FlashXflExchangeService`.
- Export:
  - document metadata
  - scenes
  - layers
  - frame labels
  - markers
  - library symbols
  - media descriptors
  - frame-sampled visual elements
- Import:
  - parse document metadata
  - parse library symbols first
  - reconstruct scenes and layers
  - rebuild frame spans from blank/content frames
  - rebuild transform/property tracks from sampled frames
  - rebuild shape keys for path geometry

## Phase 3: Unified load/save workflow

- Route `TimelineDocumentFileService` through Flash XFL.
- Support content-based XFL detection.
- Support `.fla` package load by extracting the root XFL DOM document from a ZIP
  container when present.

## Phase 4: App integration

- Add Flash XFL to the top menu import/export entries.
- Add Flash XFL to file-picker filters.
- Add Flash XFL to the interop output workspace.
- Add Flash XFL preview summaries and XML code syntax support.

## Phase 5: Validation

- Unit tests for format detection.
- Unit tests for unified open/save round-trip.
- Unit tests for XFL import/export round-trip.
- Unit tests for preview generation.
- Unit tests for `.fla` package extraction.

## ASCII mockup

```text
File
 ├─ Open...                       (*.timeline.json, *.axaml, *.svg, *.html, *.xfl, *.fla)
 ├─ Save...
 ├─ Import
 │   ├─ Avalonia XAML...
 │   ├─ SVG / SMIL...
 │   ├─ HTML / CSS...
 │   └─ Flash XFL...
 └─ Export
     ├─ Avalonia XAML...
     ├─ SVG / SMIL...
     ├─ HTML / CSS...
     └─ Flash XFL...

Output / Interop
 ┌──────────────────────────────────────────────────────────────┐
 │ Format: [ Flash XFL v ]   Layout: [ Split v ]               │
 │ Summary: XFL authoring export with scenes, symbols, labels  │
 ├──────────────────────────────┬───────────────────────────────┤
 │ XML code preview             │ Visual round-trip preview     │
 │ <DOMDocument ...>            │ Imported scene snapshot       │
 │   <timelines>                │ after XFL reparse             │
 │   <symbols>                  │                               │
 └──────────────────────────────┴───────────────────────────────┘
```

## Acceptance criteria

- A user can import an `.xfl` file and edit the result in the app.
- A user can export the current document as `.xfl`.
- A user can open/save XFL through the same document workflow as other formats.
- Library symbols, scenes, layers, frame labels, and keyframed transforms survive
  round-trip at the authoring level.
- Preview works through the existing interop preview panel.
