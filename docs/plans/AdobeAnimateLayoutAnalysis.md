# Adobe Animate Layout Analysis

Status: implemented on March 6, 2026.

Sources:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Adobe Animate workspace workflow](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)

## Goal

Align the editor shell and editing surfaces more closely to the modern Adobe Animate
workspace model while keeping the existing Dock-for-Avalonia authoring core.

The target is not a pixel copy. The target is behavioral and structural parity in the
parts that shape day-to-day animation work:

- fixed tools on the left
- document and workspace tabs at the top
- stage-centric center area
- properties-style inspector on the right
- timeline owning the full width at the bottom
- dockable, grouped, collapsible supporting panels

## Adobe Animate Traits That Matter

### 1. Permanent application frame

Animate keeps the user inside a stable authoring shell:

- menu bar at the top
- workspace chooser near the top
- document tabs directly above the active editing surface
- stage and timeline occupying the visual priority

Implication for this app:

- reduce generic organizer language
- make the shell read as a dedicated animation studio
- surface workspace presets as first-class authoring modes

### 2. Fixed left tools rail

The Tools panel is a permanent vertical rail in Animate. It is not presented as a
generic content panel and it does not compete with scene/layer/navigation panels.

Implication for this app:

- move core drawing and selection tools into a fixed narrow rail
- keep the rail visible while docks move independently
- reserve dock panels for navigation, properties, and output workflows

### 3. Right-side properties model

Animate organizes inspection around task-specific property groups. The important mental
model is:

- `Tool`
- `Object`
- `Frame`
- `Document`

Implication for this app:

- retab the inspector around those same concepts
- stop presenting the right dock as a long stack of unrelated cards

### 4. Navigator/library panel grouping

Animate groups panels into tabbed sets instead of long vertical pages:

- scenes
- layers
- library
- swatches/media/other support panels

Implication for this app:

- turn the left navigator into tabbed sections
- make scene/layer/library/media access compact and direct

### 5. Timeline dominance

Animate’s timeline is a dedicated bottom band, not just another panel. It owns width,
supports layer/frame scanning, and remains visually stable.

Implication for this app:

- keep the timeline full-width and visually heavier than side docks
- separate frame and curve editing while keeping them in the same editorial zone

### 6. Docking is visible but not noisy

Animate allows docking, regrouping, collapse, and floating, but the everyday interface
does not constantly shout about layout mechanics.

Implication for this app:

- keep show/hide/overlay/focus available
- move secondary layout actions into menus
- use top-level workspace presets and panel menus instead of many always-visible buttons

## Current Gaps In This App

### Shell gap

- the workspace still reads like a custom studio tool rather than an Animate-like authoring shell
- the dock organizer is functional but too generic

### Tooling gap

- tools are not anchored as a dedicated rail inside the dock workspace

### Navigator gap

- scenes, layers, library, and media are stacked vertically instead of grouped as tabs

### Inspector gap

- object, frame, document, and tool settings are mixed into one scroll column

### Timeline gap

- frames and curves are now separate dockables, which is good, but the surrounding shell
  still needs a more Animate-like hierarchy

## Target Layout

### Primary authoring layout

```text
+==================================================================================================+
| Menu Bar                                  [Workspace Tabs]                          [Doc Status] |
+==================================================================================================+
| Document Tabs / Context Strip                                                        [Panels v] |
+==================================================================================================+
| TT | Navigator / Library        | Stage / Source / Program Dock Area | Properties / Output      |
| OO |----------------------------|------------------------------------|--------------------------|
| OO | Scenes | Layers | Library  |                                    | Tool | Object | Frame   |
| LL | Media                     |                STAGE               | Doc  | Output / Publish  |
| SS |                           |                                    |                          |
+----+----------------------------+------------------------------------+--------------------------+
| Timeline Toolbar / Markers / Range / Playback / Zoom                                                   |
| Frames or Curves dock surface                                                                            |
+==========================================================================================================+
```

Legend:

- `TT/OO/LL/SS` is the fixed left tools rail
- navigator is compact and tabbed
- right side is property-centric and tabbed
- timeline spans the full width of the editorial floor

### Stage-focus layout

```text
+==================================================================================================+
| Workspace: Stage Focus                                                              [Panels v]   |
+==================================================================================================+
| TT | Stage dominates                                         | Properties / Source / Program     |
| OO |                                                         | as secondary dock tabs            |
| LL |                                                         |                                   |
| SS |                                                         |                                   |
+----+----------------------------------------------------------------------------------------------------+
| Timeline band remains visible but compact                                                                 |
+==========================================================================================================+
```

### Editorial / review layout

```text
+==================================================================================================+
| Workspace: Editorial / Review                                                         [Panels v] |
+==================================================================================================+
| TT | Source / Program docks | Stage document | Inspector / Output                                 |
+----+------------------------+----------------+----------------------------------------------------+
| Full-width timeline / frames / curves / markers / transport                                           |
+==========================================================================================================+
```

## Design Directives For Implementation

### Shell directives

- keep the top frame compact and professional
- show workspace presets as named tabs, not as a generic combo-first workflow
- keep document context visible

### Tool rail directives

- fixed width
- text-first compact buttons are acceptable
- keep selection and drawing tools always visible

### Panel directives

- use tabbed panel grouping for navigator and inspector
- reduce long stacked scroll columns
- keep output/publish/interchange grouped as a support panel

### Docking directives

- keep Dock-for-Avalonia as the layout engine
- use presets for the default arrangements
- preserve direct show/hide/overlay/focus actions

## Success Criteria

- the main workspace reads like an Animate-style authoring shell on first launch
- the user sees a permanent left tools rail
- the inspector maps to `Tool / Object / Frame / Doc`
- the navigator maps to `Scenes / Layers / Library / Media`
- the timeline remains the visual floor of the app
- docking remains powerful, but the visible controls become calmer and more intentional

## Implementation Result

This pass implemented:

- an Animate-style dock workspace frame with workspace tabs and context strips
- a fixed left tools rail inside the dock host
- a tabbed navigator for `Scenes / Layers / Library / Media`
- a retabbed inspector for `Tool / Object / Frame / Doc`
- grouped `Preview / Interop / Publish` support panels
