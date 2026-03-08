# Flash FLA/XFL Support Analysis

## Goal

Close the remaining practical gaps in `FLA`/`XFL` authoring support so package
round-trips are closer to Adobe Animate package behavior, especially for:

- packaged `FLA` archives based on XFL
- uncompressed `XFL` folders
- package metadata and helper files
- media payload lookup and recovery
- folder/archive conversion fidelity

## Current support

The current implementation already covers the core authoring graph well:

- `DOMDocument.xml` import/export
- library symbol XML files and `LIBRARY/manifest.xml`
- `MEDIA/manifest.xml`
- `PublishSettings.xml`
- explicit timelines, layers, symbols, motion metadata, text metadata, filters,
  vector/path data, rigging data, and document settings
- media payload packaging and reload
- preservation of unknown package files through
  `FlashAuthoringSupplementalFiles`
- conversion between compressed `FLA` archives and uncompressed `XFL` folders

## Remaining gaps

### 1. Package detection is narrower than real-world authoring folders

The package layer is still biased toward a single root file:

- folder detection requires `DOMDocument.xml` at the selected folder root
- archive loading prefers `DOMDocument.xml` and only loosely falls back to
  `.xfl`
- folder loading does not gracefully accept an authoring folder rooted by an
  openable `.xfl` document file

Impact:

- some valid authoring folders are rejected or require manual normalization
- conversion workflows are less resilient than Adobe Animate authoring folders

### 2. Package helper files are not modeled strongly enough

The package pipeline preserves extra files, but it does not treat common helper
files as first-class authoring package artifacts:

- `mimetype`
- `<document>.xfl` open-document entry
- `META-INF/metadata.xml`
- `MobileSettings.xml`

Impact:

- generated packages are missing authoring helper files commonly found in
  Animate-authored packages/folders
- imported packages preserve those files only as generic supplemental entries

### 3. Media recovery is still too path-fragile

Hydration currently relies mainly on exact `relativePath` or `frameFolder`
matches.

Impact:

- packages with renamed or normalized media paths can lose payload hydration
- `MEDIA/manifest.xml` is not used as a stronger fallback source of truth

### 4. Package preservation is not yet fully normalized

The current package builder writes core entries and then appends supplemental
files, but it does not fully normalize helper-file generation and duplicate
handling at the package level.

Impact:

- imported metadata/helper files can be overwritten by generated defaults
- folder/archive conversion fidelity can still vary depending on the source
  layout

## Refactoring and implementation direction

The remaining work should stay in the package/interchange layer and avoid UI
surface changes. The work splits cleanly into three phases:

1. package detection and helper-file fidelity
2. media manifest fallback and resilient asset lookup
3. package normalization and regression coverage

## Success criteria

- both compressed `FLA` and uncompressed `XFL` workflows load from more realistic
  authoring layouts
- package export emits helper files that make the authoring container more
  Animate-like
- imported helper files remain preserved across load/save/convert
- media payload hydration succeeds even when exact relative paths drift but the
  package still contains recoverable authoring payloads
- focused round-trip tests cover folder save/load, archive save/load, and
  archive-folder conversion
