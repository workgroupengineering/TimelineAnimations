# Flash Interop Fidelity Implementation Plan

## Phase 1: Rich XFL layer metadata

- Export/import layer compositing metadata.
- Export/import filter and color-effect metadata.
- Export/import media sync settings.
- Preserve these values on the main `TimelineLayer` model.

## Phase 2: Exact motion preservation

- Export/import explicit property tracks in XFL.
- Export/import exact keyframes with easing metadata.
- Export/import shape keyframes for morph-style path changes.
- Prefer explicit motion metadata over sampled reconstruction when importing.

## Phase 3: Packaged Flash save

- Extend unified save to accept the target file label.
- Save `.fla` as a zip package containing `DOMDocument.xml`.
- Keep `.xfl` as plain XML output.
- Keep package import compatible with the same core XFL parser.

## Phase 4: App integration

- Update file pickers so Flash save supports both `.xfl` and `.fla`.
- Update Flash XFL summaries to describe the richer fidelity.
- Keep preview on the existing round-trip pipeline.

## Phase 5: Validation

- Add tests for explicit track/easing round-trip.
- Add tests for compositing/effects round-trip.
- Add tests for media sync round-trip.
- Add tests for `.fla` package save and load.

## Acceptance criteria

- Flash XFL import/export preserves exact transform/opacity keyframes and easing.
- Shape tween keyframes survive XFL round-trip.
- Blend modes and supported effects survive XFL round-trip.
- Media sync settings survive XFL round-trip.
- Saving to `.fla` writes a zip package that can be loaded back by the app.
