# Flash XFL Current Parity Implementation Plan

## Status

Completed on March 8, 2026.

## Phase 1: Media payload and asset-item parity

Status: completed

- Add package-relative payload metadata to media assets and media frames.
- Export real media payload files into `MEDIA/`.
- Support richer XFL media item shapes:
  - `DOMSoundItem`
  - `DOMBitmapItem`
  - `DOMVideoItem`
- Hydrate packaged media payload bytes back into the editable document model on load.

## Phase 2: Publish/document metadata parity

Status: completed

- Extend `DOMDocument` export with current Animate-target metadata already modeled by the app.
- Expand `PublishSettings.xml` to preserve:
  - document target metadata
  - document 3D settings
  - publish profiles
- Import those settings back into the document model.

## Phase 3: Supplemental package retention

Status: completed

- Introduce a document-level supplemental-file model for unmodeled package files.
- Preserve unknown package entries during `FLA`/`XFL` load.
- Merge preserved entries back on save unless a generated file path replaces them.

## Phase 4: Validation

Status: completed

- Add regression coverage for:
  - `XFL` folder round-trip with real media payload files
  - packaged `FLA` round-trip with supplemental files
  - publish/document metadata round-trip
- Run build, targeted tests, and a desktop smoke launch.

## Files changed in this pass

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/FlashAuthoringSupplementalFile.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/MediaAsset.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/MediaFrameModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/TimelineDocument.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.FlashFormats/FlashAuthoringPackageService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineCoreTests.cs`

## Acceptance criteria

- Saving to packaged `FLA` or folder-based `XFL` writes actual media payload files when the document contains editable media bytes.
- Reopening those packages restores editable media payload data instead of only asset metadata.
- Publish/document settings and publish profiles survive package round-trip.
- Unknown package files are retained across load/edit/save instead of being silently discarded.
