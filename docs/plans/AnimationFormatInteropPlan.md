# Animation Format Interop Plan

## Goal

Add first-class import/export support for real animation exchange formats that map cleanly to the current keyframe editor, while keeping the editor model authoritative.

The implementation is split into four linked tracks:

1. Avalonia XAML keyframe animation import/export.
2. SVG SMIL import/export.
3. HTML/CSS keyframe import/export.
4. Avalonia toolbox expansion for controls, shapes, and visuals.

## Design Rules

- The internal `TimelineDocument` stays the source of truth.
- Exchange formats map into a single-scene document snapshot.
- When a target format cannot express internal easing exactly, bake the motion into explicit sampled keyframes.
- Keep interchange deterministic: export, re-import, and render should preserve timing, positions, opacity, rotation, dimensions, and supported control metadata.
- Prefer official format syntax over custom wrappers, but allow lightweight `data-*` metadata where a format lacks native concepts such as control kind.

## Phase 1: Model Extensions

Add model support for Avalonia-native UI elements:

- `LayerKind.AvaloniaControl`
- `AvaloniaControlKind`
- `AvaloniaControlSettings`
- `AnimationExchangeFormat`

Extend the shared snapshot/style pipeline so interchange and rendering can use the same data:

- `LayerStyle` carries control metadata.
- `LayerSnapshot` carries a cloned control snapshot.
- interpolation, cloning, symbols, and serializer paths all preserve the new data.

## Phase 2: Avalonia XAML Interop

Supported authoring/export surface:

- `Canvas` layout with absolute positioned children
- `Rectangle`, `Ellipse`, `Path`, `TextBlock`
- `Border`, `Button`, `TextBox`, `CheckBox`, `ToggleButton`, `Slider`, `ProgressBar`, `Image`
- `Canvas.Left`, `Canvas.Top`, `Width`, `Height`, `Opacity`, `RenderTransform/RotateTransform`
- `Style` selectors by element name with `Style.Animations`
- `Animation`, `KeyFrame`, `Setter`

Import rules:

- Parse supported controls and shapes from `Window`, `UserControl`, or `Canvas` roots.
- Resolve supported style animations by `#name` selector.
- Convert animated setters into internal keyframes.
- Convert `Path.Data` keyframes into shape keyframes when possible.

Export rules:

- Export the active scene to XAML.
- Emit one named visual per layer.
- Emit baked keyframes for transform/size/opacity and supported path geometry changes.

## Phase 3: SVG SMIL Interop

Supported export/import surface:

- `rect`, `ellipse`, `text`, `path`, `g`
- `animate`
- `animateTransform`
- gradient background definitions
- `data-layer-kind` and `data-control-kind` metadata on elements/groups

Import rules:

- Parse geometry, fill, stroke, absolute layout, and SMIL timing.
- Convert `d` animation values into shape keyframes for path layers.
- Rebuild Avalonia control layers from metadata-marked groups.

Export rules:

- Export the active scene as a single SVG.
- Use baked key samples for transform/opacity/size.
- Approximate Avalonia controls as SVG groups with metadata.

## Phase 4: HTML/CSS Keyframes Interop

Supported export/import surface:

- XHTML root document for XML-safe parsing
- stage container + absolute positioned child elements
- `@keyframes`
- `transform`, `left`, `top`, `width`, `height`, `opacity`
- native HTML controls for button/textbox/checkbox/range/progress
- `data-layer-kind` and `data-control-kind` metadata

Import rules:

- Parse the exported XHTML/CSS structure.
- Recover stage size and layer base styles.
- Parse `@keyframes` blocks and rebuild keyframes.

Export rules:

- Export active scene as a standalone XHTML file with embedded CSS.
- Use baked key samples so easing fidelity is preserved without depending on CSS easing parity.

## Phase 5: Avalonia Toolbox

Expand the toolbox from art primitives into a real Avalonia authoring surface:

- Shapes: rectangle, ellipse, text, path
- Visuals: border/card, image placeholder, panel
- Controls: button, text box, check box, toggle button, slider, progress bar

Acceptance:

- toolbox items can be dragged to stage
- toolbox items create real `AvaloniaControl` layers
- stage and export preview render those layers
- inspector exposes the main editable control metadata

## Acceptance Criteria

- Import/export works for Avalonia XAML, SVG, and HTML/CSS from the app UI.
- Exported files re-import into equivalent internal timelines for supported features.
- Active scene content renders consistently across stage preview and exported formats.
- Avalonia controls can be authored from the toolbox and exported to Avalonia XAML as real controls.
- Tests cover representative round-trips and parser edge cases.
