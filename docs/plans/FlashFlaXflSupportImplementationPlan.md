# Flash FLA/XFL Support Implementation Plan

## Phase 1: Package detection and helper files

Status: `completed`

### Tasks

- broaden folder detection to accept either `DOMDocument.xml` or an authoring
  `.xfl` document entry
- improve archive/document selection logic so valid `.xfl` package documents are
  accepted more reliably
- add package helper-file generation for:
  - `mimetype`
  - `<document>.xfl`
  - `META-INF/metadata.xml`
  - `MobileSettings.xml`
- classify helper files explicitly in the package layer

### Exit criteria

- exported folders and archives contain the helper files above
- folder detection succeeds for authoring folders rooted by `.xfl`

## Phase 2: Media resilience and manifest fallback

Status: `completed`

### Tasks

- parse `MEDIA/manifest.xml` into a stronger fallback metadata source
- merge manifest metadata into media assets during import
- improve package lookup for audio/image/video payloads using:
  - exact relative path
  - filename match
  - sanitized asset-name match under `MEDIA/`
  - frame-folder fallback

### Exit criteria

- media assets still hydrate when DOM media paths drift but package payloads are
  recoverable

## Phase 3: Preservation and conversion fidelity

Status: `completed`

### Tasks

- normalize package duplicate handling so helper-file defaults do not overwrite
  imported supplemental artifacts
- preserve unconsumed classified package files, not only generic `Other` files
- add focused regression coverage for:
  - folder detection through `.xfl`
  - helper-file emission
  - manifest-based media recovery
  - helper-file preservation across archive/folder conversion

### Exit criteria

- helper files and supplemental metadata survive load/save/convert round-trips
- targeted tests cover the new package-fidelity paths
