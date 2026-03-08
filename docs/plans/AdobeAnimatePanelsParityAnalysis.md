# Adobe Animate Panels Parity Analysis

## Goal

Align the docked workspace with Adobe Animate's authoring-panel model so the app exposes the same panel vocabulary and workflow structure instead of concentrating too much functionality into `Project`, `Inspector`, and `Output`.

## Sources

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Adobe Animate workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)
- [Adobe Animate authoring panels](https://helpx.adobe.com/ie/animate/using/authoring-panels.html)

## Current baseline

The app already has strong underlying authoring capability:

- stage, source, and program monitors
- frames and curves timelines
- scenes, layers, symbols, media, behaviors, actions, motion presets, component parameters
- advanced properties, effects, text, transform, hierarchy, rigging, and XFL metadata

The gap is mainly panel topology and discoverability. Adobe Animate exposes these capabilities as dedicated panels that can be shown, hidden, grouped, and floated independently. The current dock workspace still collapses many of them into three large surfaces:

- `Project`
- `Inspector`
- `Output`

That makes the workflow less Adobe-like and reduces panel parity even though the underlying features exist.

## Adobe Animate panel set relevant to this app

The official Animate workspace and authoring-panels docs consistently revolve around these panel families:

| Adobe Animate panel | Purpose in Animate | Current app status | Gap |
| --- | --- | --- | --- |
| Tools | primary authoring tool rail | Implemented | No gap |
| Stage | document canvas | Implemented | No gap |
| Timeline | frame and property editing | Implemented | No gap |
| Properties | context-sensitive selection, tool, frame, and document editing | Implemented | No gap |
| Library | symbol/asset management | Partial | Present inside `Project`, not a dedicated panel |
| Color | fill/stroke/color editing | Partial | Present inside `Inspector`, not a dedicated panel |
| Swatches | reusable palette application | Missing as panel | Need dedicated panel and swatch workflow |
| Align | alignment/distribution tools | Partial | Present inside `Inspector`, not a dedicated panel |
| Transform | object transform editing | Partial | Present inside `Inspector`, not a dedicated panel |
| Info | read-only object/stage numeric feedback | Missing as panel | Need dedicated summary panel |
| Motion Presets | store/apply motion behaviors | Partial | Exists in stage/inspector, not a dedicated panel |
| Code Snippets | snippet browser and insertion | Partial | Exists in `Motion + Actions`, not a dedicated panel |
| Components | component library and parameters | Partial | Split across `Project` and `Inspector`, not a dedicated panel |
| Movie Explorer | hierarchical document structure | Missing | Need dedicated tree panel |
| History | recent operations / undo trail | Missing | Need dedicated panel surface |
| Output / Publish | preview, interop, publish | Implemented | No gap |

## Panel parity requirements

To claim practical Animate panel parity for the dock workspace, the app needs:

1. Dedicated dock surfaces for the missing panel vocabulary.
2. Animate-like panel grouping so users can tab or float them like native panels.
3. Reuse of current authoring logic instead of duplicating editing code.
4. A real history surface and a real document-tree explorer, because those are not currently surfaced anywhere.
5. Panel menu integration so every surface can be focused, hidden, or overlaid from the main panel menus.

## Existing features that can be reused directly

These features already exist and only need panel surfacing:

- library metadata, symbol capture, placement, relink, and linkage
- component capture, parameter definitions, instance overrides
- motion preset capture/apply/delete
- ActionScript snippet browsing and insertion
- transform, align, text, color, style, gradient, and effects editing
- scene markers, frame labels, behaviors, visual states, and selection summaries

## Missing functional work required by panel parity

Some panel surfaces need new shared state, not just new XAML:

### Swatches

Needed:

- a reusable swatch collection
- commands to apply swatches to fill, stroke, gradient, and document background targets

### History

Needed:

- a recent operations journal exposed to the UI
- undo/redo entry visibility independent of toolbar buttons

### Movie Explorer

Needed:

- a hierarchical document tree built from scenes, layers, labels, markers, symbols, media, behaviors, and motion presets
- commands to focus the selected explorer node back into the authoring workspace

## Implementation strategy

### Phase 1

Introduce the shared panel state:

- swatches
- history entries
- movie explorer nodes

### Phase 2

Add new dockable surfaces:

- Library
- Color
- Swatches
- Align
- Transform
- Info
- Motion Presets
- Code Snippets
- Components
- History
- Movie Explorer

### Phase 3

Integrate the surfaces into the dock factory, menus, labels, presets, and overlay/hide flows.

### Phase 4

Add focused tests that prove:

- the new surfaces exist in the dock layout
- the new panel controls render
- swatch and explorer/history state is live

## Expected result

After this work, the docked workspace exposes the same panel language as Adobe Animate:

- asset-oriented panels on the left
- monitors and stage in the center
- inspector/design/history panels on the right
- timeline panels at the bottom
- all major authoring panels independently focusable, hideable, overlayable, and floatable
