# Adobe Animate Tools Parity Analysis

## Scope

This analysis focuses on Adobe Animate authoring-tool parity rather than broad
document, export, or workspace parity.

The current application already has:

- selection and transform handles
- rectangle, ellipse, poly-star, line, pen, brush, and text drawing
- eyedropper, paint bucket, and ink bottle style tools
- warp and rig tooling
- stage pan/zoom gestures
- quick primitive options and inspector-driven style editing

The remaining gap is that several Adobe Animate tools and tool-option workflows
still exist only as hidden gestures, partial behaviors, or are missing entirely
from the authored tool surface.

## Adobe Animate Baseline

The parity baseline for this pass comes from Adobe's official Animate product and
help documentation:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Drawing basics and tools](https://helpx.adobe.com/animate/using/drawing-basics.html)
- [Draw lines and shapes](https://helpx.adobe.com/animate/using/drawing-lines-shapes.html)
- [Selecting and transforming objects](https://helpx.adobe.com/animate/using/selecting-transforming-objects.html)
- [Asset Warp tool](https://helpx.adobe.com/sk/animate/using/asset-warp-tool.html)
- [Modern rigging with Flexi Bone](https://helpx.adobe.com/bg/animate/using/modern-rigging-felxibone-animation-tool.html)

## Current Tool Surface

Current authored tools in this app:

- Select
- Eyedropper
- Paint Bucket
- Ink Bottle
- Warp
- Rig
- Rectangle
- Ellipse
- PolyStar
- Text
- Line
- Pen
- Brush

## Gap Summary

### 1. View-navigation tools are not first-class

Adobe Animate exposes explicit tools for:

- Hand
- Zoom
- Rotate View

Current app state:

- panning and zooming exist only as gestures
- there is no explicit hand tool
- there is no explicit zoom tool with click-to-zoom or marquee zoom semantics
- there is no view-rotation tool or authored stage-view rotation state

Impact:

- the tool rail does not match Animate's mental model
- navigation behavior is discoverable only to power users
- stage-authoring parity is weaker for pen/shape-heavy workflows

### 2. Vector-selection parity is incomplete

Adobe Animate exposes:

- Selection
- Subselection
- Lasso

Current app state:

- selection exists
- direct path-point dragging exists, but only as an implicit behavior on the
  selected layer
- there is no dedicated subselection tool
- there is no lasso-based stage selection workflow

Impact:

- dense vector editing is harder than in Animate
- point-edit workflows are hidden instead of explicit
- freeform selection is missing from the stage-authoring toolkit

### 3. Pencil authoring semantics are missing

Adobe Animate distinguishes pencil-style freehand authoring from pen and brush,
including options such as:

- straighten
- smooth
- ink

Current app state:

- pen exists as sampled polyline authoring
- brush exists as freehand stroke authoring
- there is no dedicated pencil tool
- there are no authored pencil modes

Impact:

- the drawing model is less recognizable to Animate users
- tool options do not map to Adobe's established workflows

### 4. Eraser workflows are missing

Adobe Animate exposes dedicated erase workflows and tool options.

Current app state:

- there is no eraser tool
- users must delete layers or edit styles manually
- there is no stage-level erase interaction

Impact:

- editing parity is incomplete
- common corrective workflows are slower than in Animate

### 5. Tool options are too diffuse

Adobe Animate keeps the active tool visible and gives compact, mode-aware tool
options.

Current app state:

- some primitive options exist
- tool options are split across primitive quick controls, style settings, and
  hidden gestures
- there is no dedicated active-tool options surface for navigation, selection,
  pencil, or eraser workflows

Impact:

- the UI does not teach the tool model
- several capabilities feel accidental rather than authored

## Recommended Bridge

### Phase 34: Navigation Tools

Implement:

- Hand tool
- Zoom tool
- Rotate View tool
- active view-rotation state and reset path

### Phase 35: Selection Tools

Implement:

- Subselection tool
- Lasso tool
- freeform/polygon lasso modes
- contact-sensitive lasso option

### Phase 36: Drawing and Erase Tools

Implement:

- Pencil tool
- pencil modes: `Straighten`, `Smooth`, `Ink`
- Eraser tool
- eraser modes: `Object`, `Fill`, `Stroke`
- authored eraser size

### Phase 37: Tool Options and UX Parity

Implement:

- dedicated tool-options surface in the docked inspector
- active-tool summaries
- stage quick controls for view rotation and tool-sensitive options
- classic-shell and native/fallback menu parity for the new tools

## Success Criteria

The tool parity gap is bridged when:

- all core Animate authoring tools relevant to this app's vector/stage workflow
  are surfaced explicitly
- each tool has mode-aware options in the UI
- stage interactions match the selected tool rather than relying on hidden
  gestures
- the docked workspace, classic shell, and menus all expose the same tool set
- the new authoring behaviors are covered by regression tests
