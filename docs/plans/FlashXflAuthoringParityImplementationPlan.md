# Flash XFL Authoring Parity Implementation Plan

## Phase 1: Core model expansion

- Extend `LibraryItem` with linkage-sharing and Scale-9 metadata.
- Extend `TimelineLayer` with instance authoring metadata.
- Extend text styling with font family, style, alignment, spacing, and HTML-text flags.
- Keep new metadata serializable through the native JSON model and clone helpers.

## Phase 2: XFL import/export authoring metadata

- Export/import new library linkage attributes on `DOMSymbolItem`.
- Export/import Scale-9 attributes on symbols.
- Export/import instance name on `DOMSymbolInstance`.
- Export/import richer classic text attributes on `DOMStaticText`.
- Keep backward compatibility with existing simplified XFL files.

## Phase 3: Packaged Flash archive fidelity

- Write package-level `LIBRARY/*.xml` symbol files.
- Enrich `LIBRARY/manifest.xml`.
- Add a basic `PublishSettings.xml`.
- Teach package import to read symbol definitions from `LIBRARY/*.xml` when present.

## Phase 4: Editor wiring

- Add library metadata editors for linkage-sharing and Scale-9 settings.
- Add inspector editors for instance name and text authoring metadata.
- Surface the new metadata in library and layer summaries where useful.
- Keep updates routed through `MainWindowViewModel` and existing refresh/history flows.

## Phase 5: Rendering and preview updates

- Use the richer text metadata in stage rendering and export rendering.
- Keep XFL preview summaries aligned with the new feature set.

## Phase 6: Validation

- Add round-trip tests for linkage-sharing metadata.
- Add round-trip tests for Scale-9 metadata.
- Add round-trip tests for instance-name metadata.
- Add round-trip tests for classic text metadata.
- Add packaged archive tests for `LIBRARY/*.xml` and settings files.

## Acceptance criteria

- Flash XFL round-trip preserves linkage-sharing and Scale-9 symbol metadata.
- Symbol instances preserve instance names.
- Classic text preserves font family, style, alignment, spacing, and HTML-text flags.
- Packaged `.fla` output contains `DOMDocument.xml`, `LIBRARY/*.xml`, `LIBRARY/manifest.xml`, and `PublishSettings.xml`.
- Package import reconstructs symbols from package files, not only inline DOM content.
