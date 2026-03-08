# Adobe Animate Stage Zoom Overlay Plan

## Objective

Reduce the stage zoom overlay footprint so it behaves more like Adobe Animate's
compact stage magnification control while preserving:

- fit to stage
- actual size (`1:1`)
- direct zoom scrubbing
- visible zoom percentage

## Adobe Animate reference behavior

Animate treats stage magnification as a compact utility rather than a broad
toolbar block. The common pattern is:

- a small magnification readout / dropdown
- quick fit / preset access from the same compact affordance
- minimal chrome in the stage corner so the stage remains dominant

Reference pages:

- [Adobe Animate product page](https://www.adobe.com/pl/products/animate.html)
- [Use the Stage and Tools panel for Animate](https://helpx.adobe.com/animate/using/using-stage-tools-panel.html)
- [Workspace basics](https://helpx.adobe.com/pl/animate/using/workflow-workspace.html)

## Problems in the current app

1. The bottom-right zoom overlay uses wide toolbar buttons and large chips.
2. `Fit`, `1:1`, and the zoom label each occupy their own heavy segment.
3. The classic shell also spends overlay width on rotation status.
4. The current implementation is duplicated in the docked and classic stage
   surfaces instead of using a shared stage-zoom control.

## Plan

### Phase 1. Create reusable compact stage zoom control

Status: `completed`

- add a dedicated control for stage zoom chrome
- support zoom presets in a flyout
- support `Fit` and `1:1` requests without requiring separate wide buttons

### Phase 2. Replace duplicated overlay layout

Status: `completed`

- use the new control in the docked stage
- use the new control in the classic stage
- keep slider-based zoom editing

### Phase 3. Rebalance status layout

Status: `completed`

- move rotation status out of the zoom overlay
- keep rotation visible in the stage header/status chips instead

### Phase 4. Validate

Status: `completed`

- build the solution
- run a focused stage zoom control test
- smoke-launch the app
