# Flash XFL Current Support Analysis

## Status

Completed on March 8, 2026.

## Scope

This audit focuses on current Adobe Animate `XFL` authoring support in the app, not
legacy pre-XFL binary `FLA`.

## Baseline already implemented

The current `FlashXflExchangeService` already preserves a substantial part of the
Animate authoring model:

- `DOMDocument` scene/timeline/layer reconstruction
- library symbols, linkage, shared-library metadata, Scale-9 grids, and registration points
- symbol instances, button states, playback settings, and component overrides
- classic text metadata, field modes, anti-aliasing, cache-as-bitmap, and outline metadata
- explicit property tracks, easing, shape tweens, guide bindings, parenting, warp, and rig data
- Flash color effects, filters, gradient/stroke shape styling, and media sync settings
- packaged `FLA` archives and uncompressed `XFL` folders

## Current gap audit

The remaining fidelity gaps were no longer in the core timeline/symbol model. They were
in authoring package behavior around `XFL` folders and packaged `FLA` archives.

| Area | Current state before this pass | Gap |
| --- | --- | --- |
| Media payloads in packages | `MEDIA/manifest.xml` existed, but binary assets were not written or hydrated back into the editable document model | Saving and reopening packaged `XFL`/`FLA` could lose actual audio/image payloads and keep only metadata |
| Media element coverage | Import/export only used a simplified `DOMMediaItem` node | Adobe-authored `DOMSoundItem`, `DOMBitmapItem`, and `DOMVideoItem` package structures were only partially understood |
| Publish settings fidelity | `PublishSettings.xml` only stored a minimal stage summary | document target/profile metadata was not preserved through package round-trip |
| Package passthrough | Re-saving rebuilt only known files | unmodeled package files and extension metadata could be dropped on save |

## Adobe references used

- [Adobe Animate XFL / Extensible Markup Language file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Adobe Animate workspace and authoring workflow](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Adobe Animate publish settings](https://helpx.adobe.com/animate/using/publishing-documents.html)
- [Adobe Animate importing graphics, video, and audio](https://helpx.adobe.com/animate/using/import.html)

## Decision

Bridge the remaining XFL parity gap with three implementation slices:

1. package real media payloads and understand more Adobe media item node shapes
2. preserve document target and publish profile metadata through `PublishSettings.xml`
3. retain unmodeled package files so round-trip authoring does not silently discard data

## Residual hard limits

The app still cannot honestly claim parity for:

- legacy pre-XFL binary `FLA`
- undocumented or proprietary Adobe-specific package metadata that has no public contract
- runtime playback of external native video codecs when only package references are available

Those are outside the current verified/public `XFL` contract and should remain explicit
constraints instead of being implied as supported.
