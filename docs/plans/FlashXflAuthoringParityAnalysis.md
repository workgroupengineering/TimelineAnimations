# Flash XFL Authoring Parity Analysis

## Baseline

Flash XFL import/export already preserves the major timeline semantics in the app:

- scenes, layers, spans, labels, and markers
- symbols, button states, and behaviors
- explicit property tracks and easing
- shape tweens
- compositing and media-sync metadata
- packaged `.fla` save/load

The remaining parity gap is now mostly authoring metadata that Adobe Animate keeps
around symbols, instances, text, and packaged document structure.

## Remaining XFL parity gaps

| Area | Current state | Remaining gap |
| --- | --- | --- |
| Library linkage metadata | Name, folder, linkage id, source path, component flags | No base class, runtime/author-time sharing flags, shared library URL/path, export-in-first-frame, or auto-update metadata. |
| Symbol scaling metadata | Symbol kind and timeline are preserved | No Scale-9 grid authoring metadata. |
| Symbol instance metadata | Playback mode, offset, locked frame, button state | No instance name and no explicit instance authoring identity beyond the layer name. |
| Classic text metadata | Text content and font size | No font family, style, alignment, letter spacing, line spacing, or HTML-text authoring flag. |
| Packaged Flash structure | `.fla` packages include `DOMDocument.xml` plus simple manifests | Package does not emit per-library symbol XML and supporting settings files closer to Animate’s multi-file structure. |

## Adobe references used

- [Animate XML / XFL file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Working with multiple file types in Animate](https://helpx.adobe.com/animate/using/documents.html)
- [Create and duplicate symbols in Animate](https://helpx.adobe.com/animate/using/symbols.html)
- [Sharing library assets across files](https://helpx.adobe.com/vn_vi/animate/using/sharing-library-assets-across-files.html)
- [Using symbol instances in Animate](https://helpx.adobe.com/id_id/animate/using/symbol-instances.html)
- [Scaling and caching symbols on the Stage](https://helpx.adobe.com/sg/animate/using/scaling-caching-symbols.html)
- [Work with classic text in Animate](https://helpx.adobe.com/si/animate/using/classic-text.html)

## Design decision

Add a third XFL parity layer focused on authoring metadata rather than playback
reconstruction.

The new pass should:

1. preserve the authoring knobs users expect from Animate’s Library and Properties panels
2. expose those knobs in the app’s library editor and inspector
3. write them into XFL/XML in a structured way
4. improve packaged Flash output so saved archives look more like Animate authoring documents

## Target feature slice

### 1. Library linkage and shared-asset metadata

Preserve:

- linkage base class
- runtime export / runtime import flags
- export in first frame
- shared library source path / URL
- author-time auto-update

Reason:

- Animate library sharing is a core authoring workflow.
- Without these fields, linked symbols lose their intent even if the visual asset survives.

### 2. Scale-9 grid metadata

Preserve:

- scale-9 enabled flag
- left / top / right / bottom grid bounds

Reason:

- Animate treats 9-slice scaling as symbol metadata, not a stage-only effect.
- Interop should preserve the scaling contract for button and UI-like symbols.

### 3. Symbol instance metadata

Preserve:

- instance name

Reason:

- Animate’s Properties panel exposes instance naming as a first-class authoring property.
- It improves ActionScript-style targeting and makes imported documents less anonymous.

### 4. Classic text metadata

Preserve:

- font family
- bold / italic
- paragraph alignment
- letter spacing
- line spacing
- render-text-as-HTML flag

Reason:

- Animate’s classic text workflow exposes these fields directly.
- Losing them makes XFL round-trip materially incomplete for text-heavy documents.

### 5. Richer packaged Flash archive layout

Add:

- per-symbol XML files under `LIBRARY/`
- richer library manifest metadata
- basic publish-settings XML
- package import that can reconstruct library items from separate `LIBRARY/*.xml` files

Reason:

- Adobe documents are multi-file authoring packages, not just a single DOM document.
- This improves interop without requiring binary SWF/FLA internals.
