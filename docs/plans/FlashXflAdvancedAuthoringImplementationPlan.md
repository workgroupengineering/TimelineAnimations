# Flash XFL Advanced Authoring Implementation Plan

Status: completed on March 6, 2026.

## Phase 1: Model expansion

- Add guide-binding metadata to `TimelineLayer`.
- Add text-field mode metadata to `LayerTextSettings`.
- Add registration-point metadata to `LibraryItem`.

## Phase 2: XFL import/export

- Export/import guide-binding attributes on layers.
- Export/import richer text-field attributes on text elements.
- Export/import registration-point attributes on symbols.
- Keep backward compatibility with previously exported XFL.

## Phase 3: Editor wiring

- Add library editors for registration point.
- Add text-field editors for field kind, line mode, selectable, border, and variable name.
- Add guide-binding editors for eligible layers.
- Surface concise summaries where useful.

## Phase 4: Validation

- Add round-trip tests for guide-binding metadata.
- Add round-trip tests for text-field metadata.
- Add round-trip tests for registration-point metadata.
- Run build and full test validation.

## Acceptance criteria

- Flash XFL round-trip preserves guide bindings and guide options.
- Text layers preserve static/dynamic/input mode and related field metadata.
- Library symbols preserve registration-point metadata.
- The editor can inspect and modify all new fields without breaking startup or save/load.
