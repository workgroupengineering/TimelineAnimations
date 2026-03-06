# Animation Interop Expansion Plan

## Goal

Extend the current animation import/export feature set from "working interchange" to a broader, more production-friendly toolchain for Avalonia-oriented motion authoring.

## Current Baseline

- Avalonia XAML, SVG/SMIL, and HTML/CSS import/export are implemented.
- Round-trip metadata is embedded for lossless re-import.
- Fallback parsers exist for external files without embedded metadata.
- A first Avalonia toolbox is available with common controls and visual hosts.

## Remaining Gaps

- The Avalonia toolbox covers only a starter set of controls.
- Import/export does not report clearly when a file used fallback parsing or when semantics were baked/simplified.
- Avalonia project authoring still lacks a broader control palette that feels closer to a real UI composition toolbox.
- Interop users do not currently get a structured summary of what was preserved, approximated, or ignored.

## Phase A: Diagnostics And Reporting

### Objective

Make format exchange transparent instead of opaque.

### Work Items

- Add structured import/export issues with severity, source, and message.
- Report when fallback parsing is used instead of embedded metadata.
- Report when motion semantics are baked to sampled keyframes for SVG/HTML.
- Expose the last interop report in the Output panel.

### Success Criteria

- Every import/export operation yields a summary plus zero or more issues.
- The UI shows whether the operation was lossless or approximation-based.

## Phase B: Avalonia Toolbox Expansion

### Objective

Broaden the Avalonia authoring surface so the editor is useful for actual Avalonia UI compositions.

### Work Items

- Add more control kinds:
  - `ComboBox`
  - `ListBox`
  - `TabControl`
  - `Grid`
  - `StackPanel`
  - `PathIcon`
- Render those controls in the stage preview and composite renderer.
- Support them in import/export for Avalonia XAML, SVG/SMIL, and HTML/CSS.
- Add inspector editing for shared content/item metadata where applicable.

### Success Criteria

- New controls can be dragged from the toolbox, previewed, exported, and imported back.

## Phase C: Avalonia Project Integration

### Objective

Move from single animation files toward smoother Avalonia-project authoring.

### Work Items

- Support importing animations from `Window`, `UserControl`, or standalone `Canvas` roots.
- Support exporting an animation-only XAML surface and a project-friendly embedded control surface.
- Improve naming/tagging so imported controls preserve clearer provenance.

### Success Criteria

- Avalonia project files with supported structures import without manual cleanup.

## Phase D: Richer Format Mapping

### Objective

Reduce fidelity loss in fallback parsers.

### Work Items

- Improve CSS transform parsing beyond rotation-only cases.
- Improve SVG fallback parsing for grouped transforms and imported visibility.
- Add more robust parsing of control states and text/content extraction.

### Success Criteria

- Fallback imports preserve more layout and motion information from external files.

## Implementation Order

1. Phase A
2. Phase B
3. Phase C
4. Phase D

## This Pass

This implementation pass covers:

- Phase A fully
- Phase B fully for the current expanded Avalonia control set
- Phase C core import support for `Canvas`, `Window`, `UserControl`, and layout-surface roots
- Phase D first slice with richer HTML/CSS fallback parsing for transform hints and control content extraction
