# Adobe Animate Layout / UI-UX Bridge Plan

## Objective

Move the default docked authoring experience closer to modern Adobe Animate while
preserving all existing features and keeping editorial/runtime panels available.

The target is not a literal clone. The target is the same spatial model:

- narrow tool rail on the left
- compact tabbed panel groups on the sides
- dominant central stage
- wide timeline anchored across the bottom
- secondary surfaces moved behind tabs/panels instead of consuming permanent
  stage width

## Current gaps

### 1. The default `Animate` preset is still too wide in the center band

The current preset keeps source/program monitor panels visible beside the stage.
That is useful for editorial work, but it is not the default Animate authoring
shape. In Animate, the stage and timeline dominate; panel breadth is mostly
tabbed.

### 2. The tool rail is carrying non-tool content

The left rail currently includes:

- the tools
- a mode summary card
- transport buttons

Animate-style tool rails are mostly tools first, with supporting state moved to
the stage, properties, or status surfaces.

### 3. Stage chrome is still too heavy

The stage header still exposes too many controls at once. Many of those controls
are valid, but they should be grouped into compact strips rather than presented
as broad, equally weighted rows.

### 4. Side panels are visually heavier than necessary

The project and inspector surfaces use stacked cards well, but the command rows
and spacing still read more like general app panels than Animate authoring
panels.

## Animate-oriented target layout

```text
+----------------------------------------------------------------------------------+
| Native menu / title chrome                                                       |
+----------------------------------------------------------------------------------+
| Workspace strip: Animate | Editorial | Review | Stage Focus | Panels | More     |
+----------------------------------------------------------------------------------+
| TL | Project / Library / Components |              Stage              | Props... |
|    |                                |                                 | Color... |
|    |                                |                                 | Align... |
|    |                                |                                 | Hist...  |
|    |                                |                                 | Output   |
+----------------------------------------------------------------------------------+
|                         Frames / Curves / Motion bottom workspace                |
+----------------------------------------------------------------------------------+
```

Key behavior rules:

- `Animate` preset: stage and timeline prioritized; source/program moved into
  tabbed side panels instead of a permanent monitor split.
- `Editorial` preset: source/program can remain visible as first-class monitor
  surfaces.
- tool rail contains tools and only minimal transport/utility controls.
- stage header becomes a compact authoring strip with grouped quick controls.

## Implementation phases

### Phase 1. Rebalance dock presets

Status: `completed`

- make `Animate` preset stage-first
- move source/program/output/history into the right tab group for that preset
- reduce left/right default proportions
- give the bottom timeline slightly more height in `Animate`

### Phase 2. Slim the tool rail

Status: `completed`

- remove the tool summary card from the rail
- group tools into compact sections:
  - selection/navigation
  - drawing/editing
  - rig/warp/authoring
- keep compact transport buttons at the bottom only

### Phase 3. Compact the stage shell

Status: `completed`

- reduce the stage header to:
  - context + transport + snap/onion + metrics
  - compact segmented quick-option strip
- keep shape/tool/motion options, but visually subordinate them
- use horizontal scrolling only for the quick-option strip, not for primary
  context

### Phase 4. Tighten side-panel density

Status: `completed`

- reduce command-row visual weight in project/assets panels
- keep inspector/project sections but align padding and spacing more closely to
  authoring-tool panels
- preserve all current tabs and commands

## Validation

- build
- smoke-launch
- verify that `Animate` preset opens with more stage area than before
- verify that all monitor/panel surfaces remain reachable via the dock menus and
  workspace presets
