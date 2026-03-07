# UI/UX Control Parity Retemplating Plan

## Goal

Bring the application's UI control system closer to the authoring ergonomics of:

- Adobe Animate / Macromedia Flash
- Adobe Premiere Pro
- Microsoft Expression Blend

This plan is focused on UI/UX controls, theming, panel chrome, density, hierarchy,
and editing ergonomics rather than feature parity in the animation engine.

The implementation baseline is Avalonia `FluentTheme`, but every visible control
family should be reskinned through app-owned `ControlTheme` definitions and compact
design tokens instead of relying on default Fluent visuals.

## External reference points

- Adobe Animate workspaces:
  [helpx.adobe.com/animate/using/workspaces.html](https://helpx.adobe.com/animate/using/workspaces.html)
- Adobe Animate timeline/layer workflows:
  [helpx.adobe.com/animate/using/timeline-layers.html](https://helpx.adobe.com/animate/using/timeline-layers.html)
- Adobe Premiere Pro workspaces and panel layouts:
  [helpx.adobe.com/premiere-pro/using/workspaces.html](https://helpx.adobe.com/premiere-pro/using/workspaces.html)
- Microsoft control customization / Blend-compatible parts-and-states model:
  [learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/creating-a-control-that-has-a-customizable-appearance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/creating-a-control-that-has-a-customizable-appearance)
- XAML styles and template editing workflow:
  [learn.microsoft.com/en-us/windows/apps/design/style/xaml-styles](https://learn.microsoft.com/en-us/windows/apps/design/style/xaml-styles)

## Current gap analysis

### 1. Global shell and chrome

Current state:

- The shell is functionally dense but still reads as a collection of styled panels.
- Header strips, dock tabs, panel headers, monitors, inspector sections, and timeline
  chrome do not yet feel like one unified professional control language.

Parity target:

- Animate-style dense studio chrome.
- Premiere-style panel grouping and workspace legibility.
- Blend-style clear sectioning between toolbox, artboard, properties, states, and code.

### 2. Control theme architecture

Current state:

- Most controls are customized through broad `Style` selectors.
- The app has limited use of app-owned `ControlTheme` definitions.
- Fluent resources are not fully overridden for a product-specific dense language.

Parity target:

- App-owned `ControlTheme` definitions for every major control family.
- Fluent as behavioral/template baseline, but with app-specific control visual identity.
- Compact metrics, stronger hover/selection/focus states, and consistent chrome tokens.

### 3. Menus, toolbars, and command surfaces

Current state:

- Menus are complete functionally, but top-level menus, flyouts, toolbar buttons, and
  timeline menus still look close to default controls with color overrides.

Parity target:

- Strong command hierarchy.
- Dense menu rows with clear accelerators and submenu affordances.
- Distinct toolbar, transport, timeline action, and dock-header button treatments.

### 4. Dock tabs, panel headers, and inspector forms

Current state:

- Tabs and panel headers do not fully communicate active context.
- Inspector sections have useful content but not enough product-grade hierarchy.
- Inputs are compact, but focus/selection states are not expressive enough.

Parity target:

- Premiere/Animate style dock tabs and grouped headers.
- Blend-style property cards with crisp labels, section headers, and dense editing rows.
- Better form affordances for text inputs, combos, toggles, and checkboxes.

### 5. Utility controls

Current state:

- Scrollbars, splitters, sliders, list items, popups, and tooltips are serviceable but
  still generic.

Parity target:

- Compact but discoverable utility controls.
- Better splitter visibility while dragging/hovering.
- Modern thin scrollbars and precise timeline/stage slider treatments.

## Implementation phases

### Phase 1. Design tokens and Fluent override baseline (completed)

- Define compact global metrics for heights, paddings, radii, and typography.
- Override Fluent resource keys for buttons, text controls, combos, tabs, sliders,
  menus, overlays, and scrollbars.
- Normalize panel/background/chrome palette into a single system.

### Phase 2. Product control themes (completed)

- Add app-owned `ControlTheme` definitions for:
  - `Button`
  - `ToggleButton`
  - `TextBox`
  - `ComboBox`
  - `CheckBox`
  - `TabItem`
  - `MenuItem`
  - `ScrollBar`
- Apply these globally through `Theme` setters and class-based variants.

### Phase 3. Shell and dock chrome (completed)

- Rework top menu, workspace strip, dock headers, edge handles, panel tabs, and
  monitor/timeline headers to use the same compact control language.
- Improve visual grouping and selection clarity.

### Phase 4. Inspector and editing surfaces (completed)

- Tighten inspector sections, property cards, color chips, and form rows.
- Improve monitor shells, canvas overlay chrome, and timeline header hierarchy.

### Phase 5. Utility polish (completed)

- Refine list row states, scrollbars, grid splitters, tooltips, and context menus.
- Ensure hover/focus/pressed states remain readable on dense dark surfaces.

## Completion update

This plan is now fully implemented:

- Phase 1 fully
- Phase 2 fully
- Phase 3 fully
- Phase 4 fully
- Phase 5 fully within the current Dock-for-Avalonia workspace architecture

## Success criteria

- The workspace reads as one coherent studio shell rather than styled default controls.
- Primary controls remain compact without becoming hard to hit.
- Top-level menus, inspector inputs, dock tabs, timeline actions, and scroll surfaces
  all share the same visual language.
- Selection, focus, hover, and pressed states are clearer than the current build.
- Stage and timeline remain the dominant surfaces, with support panels visually quieter
  but more legible.
