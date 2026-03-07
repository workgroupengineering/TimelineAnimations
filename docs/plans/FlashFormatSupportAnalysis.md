# Flash Format Support Analysis

## Goal

Add editable Flash-authoring interchange to TimelineAnimations without pulling in
copyleft code or runtime-only SWF tooling that does not map to the app's scene,
layer, symbol, and keyframe model.

## External analysis

### Adobe-supported authoring format

Adobe Animate's documented open authoring interchange is XFL:

- [Adobe Animate XFL overview](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)

Key points that matter for this app:

- XFL is XML-based.
- Adobe positions XFL as the internal/open representation behind Animate files.
- XFL is the authoring-safe interchange path for symbols, scenes, timelines, and
  document structure.

### Candidate open-source libraries

| Candidate | License | Verdict | Reason |
| --- | --- | --- | --- |
| `openfl/swf` | MIT | Rejected for direct use | Permissive, but focused on SWF parsing/runtime display data in the Haxe ecosystem, not editable authoring-grade XFL import/export for a C# Avalonia app. |
| SWF decompiler / reverse-engineering tools | Often GPL/LGPL or mixed | Rejected | License mismatch and wrong abstraction level for editable scene/timeline workflows. |
| Authoring-grade XFL C# library | No practical permissive option found | Not available | No maintained permissive C# library was found that covers scenes, timelines, symbols, and editable round-trip authoring semantics. |

## Decision

Implement Flash authoring support in-house as an XFL profile.

Why this is the correct path:

- XFL matches the app's authoring model better than binary SWF.
- It avoids copyleft/license contamination.
- It keeps the implementation in C# and under project control.
- It maps to existing features already present in the editor:
  - scenes
  - layers and folders
  - frame-based timelines
  - symbols and library items
  - frame labels
  - media placeholders
  - editable stage geometry

## Scope of the implementation

### Supported

- Flash XFL import
- Flash XFL export
- Flash XFL open/save through the unified document workflow
- Editing after import using the existing app model
- Library symbol import/export
- Scene timeline import/export
- Frame labels
- Blank/content frame spans
- Shape, text, symbol-instance, control, and media-placeholder interchange
- Preview round-trip through the existing interop preview surface

### Deliberate design choice

The app uses a self-contained single-file XFL DOMDocument profile so it fits the
existing code-preview, import/export, and unified file workflow. Motion is written
as explicit frame content rather than opaque binary/runtime data.

### Non-goals in this pass

- Native SWF bytecode editing
- Full ActionScript compiler/runtime parity
- External binary media payload packing inside the XFL export
- Adobe-only proprietary authoring edge cases not representable in the current
  document model

## Mapping strategy

| TimelineAnimations model | XFL representation |
| --- | --- |
| `TimelineDocument` | `DOMDocument` |
| `SceneModel` | `DOMTimeline` |
| `TimelineLayer` | `DOMLayer` |
| Frame spans | `DOMFrame index/duration/isBlank` |
| Shape/text/control/media visual | `DOMShape` / `DOMStaticText` / `DOMComponentInstance` / `DOMMediaInstance` |
| Linked symbol instance | `DOMSymbolInstance` |
| `LibraryItem` | `DOMSymbolItem` |
| `FrameLabelModel` | `DOMFrameLabel` |
| Scene markers | `DOMMarker` |

## Compatibility stance

The implementation targets editable authoring interchange and stable round-trip in
TimelineAnimations first. The emitted XML follows Animate/XFL naming and structure
closely enough to be recognizable, but this pass optimizes for deterministic import,
export, preview, and editing inside this app.
