# TimelineAnimations Studio

TimelineAnimations Studio is a desktop animation authoring environment built with
Avalonia and C#. It combines timeline-based animation editing, a dockable
workspace, stage-first visual editing, and multi-format interchange for native
project documents, Avalonia XAML animations, SVG/SMIL, HTML/CSS, and Adobe
Animate / Flash XFL.

The project is intentionally positioned between a motion editor, a keyframe
timeline tool, and an Adobe Animate-style authoring environment. The current
feature set covers the major authoring areas expected from Flash/Animate-class
tools and extends them with Avalonia-native workflows.

## Why This Project Exists

TimelineAnimations Studio exists to provide a modern, cross-platform animation
editor for Avalonia applications and general motion authoring workflows, while
remaining familiar to users coming from:

- Adobe Animate
- legacy Macromedia Flash authoring
- Premiere-style editorial timelines
- Blend-style state and interaction authoring

It is not an Adobe product, and it does not attempt byte-for-byte or runtime
engine parity with every Adobe export target. The focus is authoring parity,
editable interchange, and a production-grade workflow inside an Avalonia-first
application.

## Highlights

- WYSIWYG stage editing with live timeline playback
- dockable workspace with stage, source/program monitors, frames, curves,
  project, inspector, and output surfaces
- dedicated Adobe Animate-style panel set including Library, Color, Swatches,
  Align, Transform, Info, Components, History, Motion Presets, Code Snippets,
  and Movie Explorer dock surfaces
- native menu support on supported platforms with in-window fallback elsewhere
- standalone rendering stack with selectable `Avalonia` and `SkiaSharp` engines
- scene-based multi-shot authoring with markers, transitions, and work areas
- library, symbols, components, nested editing, and visual states
- vector drawing, paint/style tools, primitive shapes, motion tweens, shape keys,
  and path editing
- Animate-style vector point editing with Bezier handles, handle-mode switching,
  path cleanup commands, gradient transform editing, and variable-width stroke data
- Animate-style tool surface with Hand, Zoom, Rotate View, Subselect, Lasso,
  Pencil, and Eraser workflows plus mode-aware tool options
- Animate-style 3D transform authoring with document perspective and vanishing-point controls
- transform parenting, warp pins, rig bones, and pose capture/apply workflows
- motion presets, advanced onion-skin controls, and stage-adjacent quick property strips
- Flash-style effects including bevel, gradient glow, gradient bevel, contrast, and hue shift
- media clips, Flash-style sound sync modes, audio finishing, editorial operations, and publish profiles
- Animate-style document targets and target-aware script snippets
- component parameter definitions with per-instance overrides and nested symbol substitution
- first-class Avalonia control toolbox and Avalonia animation interchange
- Flash XFL import/export/open/save with editable authoring metadata, packaged archives, and XFL-folder conversion workflows
- runtime Avalonia XAML preview plus bitmap preview through the selected rendering engine

## Feature Inventory

The table below summarizes the current application surface by area.

| Area | What the program includes | Notes |
| --- | --- | --- |
| Workspace and shell | Native menu on supported platforms, integrated title-bar support, docked workspace presets, floating tool windows, overlay/hide/show panel modes, persistent layouts, compact Premiere/Animate-style shell organization | Built on Avalonia plus Dock for Avalonia |
| Panels and authoring surfaces | Dedicated Project, Library, Components, Movie Explorer, Inspector, Color, Swatches, Align, Transform, Info, Motion Presets, Code Snippets, History, Output, Stage, Source, Program, Frames, and Curves panels | Panels are dockable, floatable, hideable, overlay-capable where appropriate, and grouped like Animate authoring workspaces |
| Stage and canvas | Resizable scene canvas, fit/actual-size zoom, explicit hand/zoom tools, rotated view with reset, pan/zoom gestures, snapping, onion skin, advanced onion controls, selection handles, resize handles, stage overlays, stage quick-property strips | Editor canvas is a first-class authoring surface |
| Scenes and document model | Multi-scene documents, active scene switching, scene order changes, scene duration/FPS/canvas settings, background gradients, scene markers, in/out/work area, scene transitions | Supports stage-level authoring and editorial playback semantics |
| Drawing tools | Select, subselect, lasso, hand, zoom, rotate view, rectangle, ellipse, poly star, text, line, pen, pencil, brush, eraser, eyedropper, paint bucket, ink bottle, warp, rig | Includes tool-option modes for zoom, lasso, pencil, eraser, and stage-view rotation |
| Vector authoring | Path layers, anchor and Bezier-handle editing, corner/smooth point modes, per-point stroke width scale, insert/delete/reverse/smooth/straighten/optimize path commands, merge/object/primitive drawing modes, per-corner rectangle radii, ellipse start/sweep, poly-star metadata, gradient fills with center/scale transforms, stroke cap/join/miter control, shape keys, morphing, motion paths | Supports retained vector authoring close to Adobe Animate’s direct path-editing workflow |
| Timeline and curves | Frame spans, blank cels, holds, labels, markers, keyframes, property curves, easing curves, 3D transform tracks, auto-key, motion presets, playback controls, timeline zoom, frame zoom, range copy/paste, onion keyframes-only/base opacity/falloff controls | Frames and curves are separate surfaces in the docked workspace |
| Power editing | Layer folders, hierarchy flattening, edit multiple frames, razor, slip, slide, ripple, marker-aware editorial operations | Inspired by Flash, Premiere, and Blend workflows |
| Library and symbols | Graphic/movie clip/button symbols, linkage metadata, scale-9 metadata, registration points, relinking, library folders and metadata, symbol isolation editing | Supports reusable authoring assets |
| Components and states | Component instances, component parameter definitions, per-instance overrides, behavior triggers/actions, visual state groups, state capture/apply, button states, prototype preview | Blend-style state authoring plus Animate-style configurable components |
| Text authoring | Classic text metadata, dynamic/input/static field modes, alignment, bold/italic, variable names, max chars, password, device fonts, anti-alias modes, HTML text flag | Flash-style text field metadata is preserved in XFL |
| Avalonia toolbox | Border, Button, TextBlock, TextBox, CheckBox, ToggleButton, Slider, ProgressBar, Image, ComboBox, ListBox, TabControl, Grid, StackPanel, PathIcon, Panel | Intended for Avalonia project workflows |
| Parenting, warp, and rigging | Transform-parent selection, parent-aware rendering, warp mesh settings, warp pins, rig bones, pose capture/apply/delete, stage manipulation handles | Implemented across models, rendering, editor UI, and XFL |
| 3D transform authoring | Rotation X/Y, Z depth, document 3D enablement, perspective angle, vanishing point X/Y, 3D-aware property tracks, flattened preview/export, XFL round-trip | Focused on Animate-style stage authoring parity |
| Compositing and effects | Blend modes, masks, camera layers, blur, glow, shadow, tint, brightness, saturation, contrast, hue shift, bevel, gradient glow, gradient bevel, Flash color effects, cache-as-bitmap metadata | Shared across preview, export, and XFL |
| Media and audio | Audio/video/image-sequence media assets, clip timing, `Start`/`Event`/`Stop`/`Stream` sound sync, repeat count, repeat forever, waveform preview, gain/pan/fades, meters, mute/solo, work-area-aware playback | Editorial monitoring and Flash-style sound workflows are included |
| Document targets and scripting | Animate-style document targets (`HTML5 Canvas`, `WebGL`, `ActionScript 3`, `AIR`), behavior triggers, actions, frame scripts, action snippets, target-aware snippets, prototype runtime integration, script validation | Actions panel and snippet workflows are available |
| Rendering engines | Standalone reusable render contracts, selectable `Avalonia` and `SkiaSharp` backends, shared request model, persisted renderer choice, unified stage/export/preview routing | `SkiaSharp` renders common authored content natively and falls back to Avalonia for unsupported high-complexity samples |
| Import/export and file workflow | Native project JSON, unified open/save for XAML/XFL/SVG/HTML, preview code surfaces, runtime Avalonia preview, XFL package support, packaged `.fla`/`.xfl` handling, target-format-only interchange export payloads | Interchange exports no longer embed the full project |
| Publish and render | Publish profiles, validation, preview, PNG sequence, sprite sheet, GIF, MP4, JSON scene graph, package output, self-contained desktop packaging scripts | GIF and MP4 publishing require `ffmpeg`; desktop packaging uses the `builds` scripts/workflows |
| Testing and validation | Unit tests, service tests, Avalonia headless coverage, dock rendering checks, interop round-trip tests | Headless font availability still matters for some UI tests |

## File Formats and Output Support

| Workflow | Native project model | Avalonia XAML | Flash XFL / FLA package | SVG / SMIL | HTML / CSS | PNG frame | PNG sequence | Publish profiles |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Open | Yes | Yes | Yes | Yes | Yes | No | No | n/a |
| Save | Yes | Yes | Yes | Yes | Yes | No | No | n/a |
| Import | Yes | Yes | Yes | Yes | Yes | Audio/video/media import handled separately | Media import handled separately | n/a |
| Export | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Visual preview | Native editor | Yes | Yes | Yes | Yes | n/a | n/a | Yes |
| Editable round-trip intent | Full | Best-effort plus metadata-aware import | Broad authoring fidelity | Best-effort | Best-effort | Render output only | Render output only | Output packaging and validation |

## Comparison Baseline

This README compares TimelineAnimations Studio against:

- Adobe Animate current public feature surface as reviewed from Adobe product and
  HelpX documentation on 2026-03-07
- Macromedia Flash 8 (2005), the final Macromedia-branded Flash release before
  Adobe branding replaced the product line

That means "Macromedia Flash latest version" in this document refers to Flash 8,
because there is no newer Macromedia-branded release after the Adobe acquisition.

## Comparison to Adobe Animate and Macromedia Flash

| Capability area | TimelineAnimations Studio | Adobe Animate (current public product/help surface) | Macromedia Flash 8 | Comparison summary |
| --- | --- | --- | --- | --- |
| Multi-scene authoring | Yes | Yes | Yes | At parity for practical scene/timeline organization |
| Dockable authoring workspace | Yes | Yes | Limited legacy paneling | Closer to modern Animate than legacy Flash |
| Dedicated authoring panel set (`Library`, `Color`, `Swatches`, `Align`, `Transform`, `Info`, `History`, `Motion Presets`, `Code Snippets`, `Movie Explorer`) | Yes | Yes | Partial legacy panel set | Now aligned much more closely with Animate than earlier app revisions or Flash 8 |
| Native menu plus title-bar shell integration | Yes | Platform-native shell behavior | Legacy OS-native menus, older shell model | Modern cross-platform shell with native/fallback strategy |
| Stage editing and live timeline | Yes | Yes | Yes | Full core authoring parity |
| Frame spans, cels, labels, onion skin | Yes | Yes | Yes | Full core timeline parity |
| Motion presets and advanced onion controls | Yes | Yes | More limited legacy workflow | Closer to current Animate than Flash 8 |
| Property curves and easing editor | Yes | Yes | Limited compared with modern Animate | Exceeds Flash 8 |
| Navigation tool surface (`Hand`, `Zoom`, `Rotate View`) | Yes | Yes | More gesture/legacy workflow oriented | Closer to current Animate |
| Selection and correction tools (`Subselect`, `Lasso`, `Pencil`, `Eraser`) | Yes | Yes | More limited legacy tool UX | Closer to current Animate |
| 3D transform authoring and perspective | Yes | Yes | No | At parity in the major modern Animate category |
| Source/program monitor workflow | Yes | Not a Premiere-style dual-monitor workflow | No | Extends beyond classic Flash |
| Symbols, library, components | Yes | Yes | Yes | Full major category parity |
| Component parameter definitions and per-instance overrides | Yes | Yes | Limited legacy component workflows | Closer to current Animate than Flash 8 |
| Nested symbol editing | Yes | Yes | Yes | Full major category parity |
| Primitive shape authoring and poly-star tools | Yes | Yes | Basic primitive workflows | Closer to current Animate |
| Direct vector path editing with Bezier handles and smooth/corner point modes | Yes | Yes | More limited legacy path editing | At parity in the core authoring workflow |
| Gradient transform editing for vector fills | Yes | Yes | More limited legacy fill tooling | Closer to current Animate |
| Variable-width vector stroke authoring data | Yes | Yes | Limited older workflow | Closer to current Animate |
| Direct style tools (`Eyedropper`, `Paint Bucket`, `Ink Bottle`) | Yes | Yes | Yes | Full major category parity |
| Animate document targets (`HTML5 Canvas`, `WebGL`, `ActionScript 3`, `AIR`) | Yes | Yes | No | Closer to current Animate than Flash 8 |
| Action snippets and script insertion | Yes | Yes | Limited older workflow | Closer to current Animate |
| Visual states and interaction behaviors | Yes | Partial through Animate plus custom workflows | Limited | Extends beyond Flash 8 and overlaps Blend-style tooling |
| Layer folders and power editing | Yes | Yes | Basic | Exceeds Flash 8 |
| Editorial operations (`razor`, `slip`, `slide`, `ripple`) | Yes | Animate is not Premiere | No | Extends beyond Animate/Flash authoring scope |
| Camera layers and scene transitions | Yes | Yes | No modern camera workflow | Closer to current Animate |
| Effects and filter stack | Yes | Yes | Broad legacy filter feature set | At parity in major category, implementation details differ |
| Flash-style sound sync (`Start`, `Event`, `Stop`, `Stream`, repeat) | Yes | Yes | Yes, with older authoring workflows | At parity in major category |
| Audio finishing and meters | Yes | Partial | Basic | Stronger editorial/audio tooling than Flash 8 |
| Transform parenting | Yes | Yes | No | At parity with modern Animate category |
| Asset warp | Yes | Yes | No | At parity with modern Animate category |
| Modern rigging / flexi-bone style pose workflows | Yes, editor-native foundation | Yes | No | At parity in major category, implementation details differ |
| Flash XFL authoring interchange | Yes | Native | No XFL in Flash 8 era | Supports modern Adobe authoring interchange |
| Pluggable renderer backends | Yes (`Avalonia`, `SkiaSharp`) | Proprietary renderer stack | Proprietary legacy renderer stack | Unique engine-selection workflow in this project |
| Avalonia control toolbox and Avalonia animation interop | Yes | No | No | Unique to this project |
| HTML/CSS, SVG, and Avalonia code preview | Yes | HTML5 target workflows exist, but not Avalonia | No | Unique multi-platform authoring value |
| Native project model plus publish profiles | Yes | Yes | Limited legacy publish workflows | Modernized authoring/publish approach |

## What "Parity" Means Here

For this project, parity means:

- a first-class authoring workflow exists in the editor
- the underlying document model supports the feature
- the feature is surfaced in the UI
- preview and export understand the feature where applicable
- interchange preserves the feature where the target format supports it

Parity does not mean byte-identical Adobe runtime behavior, proprietary file
equivalence, or complete replacement of every Adobe export/runtime stack.

## Technology Stack

| Layer | Technology |
| --- | --- |
| Desktop UI | Avalonia 11 |
| Theme base | Fluent theme for Avalonia |
| Rendering contracts | `TimelineAnimations.Rendering` |
| Avalonia renderer | `TimelineAnimations.Rendering.Avalonia` |
| Skia renderer | `TimelineAnimations.Rendering.SkiaSharp` + `SkiaSharp` |
| Text/code editing | AvaloniaEdit + TextMate |
| Docking | Dock for Avalonia |
| MVVM helpers | CommunityToolkit.Mvvm |
| Core model and services | .NET 9 class library |
| Flash package container layer | `TimelineAnimations.FlashFormats` |
| Tests | xUnit + Avalonia.Headless |

## Repository Layout

| Path | Responsibility |
| --- | --- |
| `src/TimelineAnimations.Core` | Domain models, document model, animation math, interchange, publish validation, authoring services |
| `src/TimelineAnimations.FlashFormats` | XFL/FLA package container detection, supplemental file preservation, and archive/folder conversion support |
| `src/TimelineAnimations.Rendering` | Renderer-neutral contracts, render requests, frame results, and shared transform/request services |
| `src/TimelineAnimations.Rendering.Avalonia` | Avalonia-based rendering backend and Avalonia-specific render helpers |
| `src/TimelineAnimations.Rendering.SkiaSharp` | SkiaSharp-based rendering backend and Skia-specific render helpers |
| `src/TimelineAnimations.App` | Avalonia application, views, dock workspace, stage controls, renderer host integration, and UI services |
| `tests/TimelineAnimations.App.Tests` | Unit tests, integration tests, and Avalonia headless UI tests |
| `builds` | Version metadata, publish scripts, and release packaging helpers |
| `.github/workflows` | CI matrix build/test workflow and tagged release packaging workflow |
| `docs/plans` | Architecture notes, parity analysis, implementation plans, and feature roadmaps |

## Getting Started

### Prerequisites

- .NET 9 SDK
- macOS, Windows, or Linux capable of running Avalonia desktop applications
- `ffmpeg` on `PATH` if you want GIF or MP4 publish output

### Build

```bash
dotnet build TimelineAnimations.sln
```

### Run

```bash
dotnet run --project src/TimelineAnimations.App
```

### Test

```bash
dotnet test TimelineAnimations.sln
```

## Workflow Summary

1. Create a blank animation or load the sample composition.
2. Choose or adjust the Animate-style document target/profile for the project.
3. Author scenes, layers, symbols, components, and media.
4. Edit on the stage with direct manipulation, paint/style tools, and timeline playback.
5. Use Animate-style panels such as Library, Color, Swatches, Align, Transform, Info, Motion Presets, Code Snippets, History, and Movie Explorer as needed.
6. Use explicit authoring tools such as Hand, Zoom, Rotate View, Subselect, Lasso, Pencil, and Eraser where needed.
7. For vector paths, edit anchors and Bezier handles directly on stage, then refine point mode, width, cleanup, and gradient transform settings in the inspector.
8. Use frames, curves, motion presets, parenting, warp, and rigging workflows.
9. Choose the active rendering engine when you want to compare Avalonia and SkiaSharp output paths.
10. Configure Animate-style 3D, sound, and component parameter workflows where needed.
11. Preview code, runtime Avalonia XAML, or bitmap output through the selected render engine, then round-trip through supported interchange formats.
12. Export frames, sequences, publish profiles, or packaged desktop builds.

## Key Documentation

- Adobe Animate gap analysis:
  - `docs/plans/AdobeAnimateFeatureAnalysis.md`
- Adobe Animate bridge roadmap:
  - `docs/plans/AdobeAnimateGapBridgePlan.md`
- Adobe Animate effects, shapes, timeline UX analysis:
  - `docs/plans/AdobeAnimateEffectsShapesTimelineUxAnalysis.md`
- Adobe Animate effects, shapes, timeline UX implementation plan:
  - `docs/plans/AdobeAnimateEffectsShapesTimelineUxImplementationPlan.md`
- Adobe Animate FLX/XFL parity analysis:
  - `docs/plans/AdobeAnimateFlxParityAnalysis.md`
- Adobe Animate FLX/XFL parity implementation plan:
  - `docs/plans/AdobeAnimateFlxParityImplementationPlan.md`
- Adobe Animate tools parity analysis:
  - `docs/plans/AdobeAnimateToolsParityAnalysis.md`
- Adobe Animate tools parity implementation plan:
  - `docs/plans/AdobeAnimateToolsParityImplementationPlan.md`
- Adobe Animate panels parity analysis:
  - `docs/plans/AdobeAnimatePanelsParityAnalysis.md`
- Adobe Animate panels parity implementation plan:
  - `docs/plans/AdobeAnimatePanelsParityImplementationPlan.md`
- Adobe Animate vector-editing parity analysis:
  - `docs/plans/AdobeAnimateVectorEditingParityAnalysis.md`
- Adobe Animate vector-editing parity implementation plan:
  - `docs/plans/AdobeAnimateVectorEditingParityImplementationPlan.md`
- Rendering engine refactor analysis:
  - `docs/plans/RenderingEngineRefactorAnalysis.md`
- Rendering engine refactor implementation plan:
  - `docs/plans/RenderingEngineRefactorImplementationPlan.md`
- Flash FLA/XFL packaging analysis:
  - `docs/plans/FlashFlaParityAnalysis.md`
- Flash FLA/XFL packaging implementation plan:
  - `docs/plans/FlashFlaParityImplementationPlan.md`
- Flash XFL parity plans:
  - `docs/plans/FlashFormatSupportAnalysis.md`
  - `docs/plans/FlashXflCurrentSupportAnalysis.md`
  - `docs/plans/FlashXflCurrentParityImplementationPlan.md`
  - `docs/plans/FlashInteropFidelityAnalysis.md`
  - `docs/plans/FlashXflTransformParityImplementationPlan.md`
- Cross-tool parity:
  - `docs/plans/CrossToolParityRoadmap.md`

## Known Limitations and Environment Notes

- Full-suite Avalonia headless rendering can depend on system-font availability in
  some macOS environments. The latest known full-suite baseline is `143/147`,
  with 4 remaining failures caused by `fonts:SystemFonts` resolution in dock
  rendering tests. More recent work, including the latest vector-editing parity
  pass, has been validated with passing builds, focused regressions, and local
  smoke runs on the affected feature areas.
- The `SkiaSharp` renderer is fully selectable in the app and renders common
  authored content natively, but still falls back to the Avalonia backend for
  unsupported high-complexity samples such as effect-heavy layers, deformed
  geometry, and media-backed video content.
- GIF and MP4 publish outputs require `ffmpeg`.
- Adobe Animate and Flash comparisons are capability-oriented, not proprietary
  runtime or binary-format equivalence claims.

## License

This repository is released under the MIT License. See `LICENSE`.

## Reference Baseline

The comparison and parity positioning in this README is grounded in official
Adobe product/help material plus the final Macromedia Flash release context:

- Adobe Animate product page:
  - <https://www.adobe.com/pl/products/animate.html>
- Adobe Animate workspace/help:
  - <https://helpx.adobe.com/pl/animate/using/workflow-workspace.html>
- Adobe Animate document targets:
  - <https://helpx.adobe.com/ie/animate/using/HTML5-Canvas.html>
  - <https://helpx.adobe.com/sk/animate/using/actionscript.html>
  - <https://helpx.adobe.com/ie/animate/using/create-webgl-documents.html>
- Adobe Animate authoring features:
  - <https://helpx.adobe.com/animate/using/drawing.html>
  - <https://helpx.adobe.com/animate/using/painting-tools.html>
  - <https://helpx.adobe.com/animate/using/writing-scripts.html>
  - <https://helpx.adobe.com/animate/using/layer-parenting-in-adobe-animate.html>
  - <https://helpx.adobe.com/sk/animate/using/asset-warp-tool.html>
  - <https://helpx.adobe.com/bg/animate/using/modern-rigging-felxibone-animation-tool.html>
- Adobe Animate maintenance-mode announcement:
  - <https://helpx.adobe.com/ie/animate/kb/faq-for-adobe-animate-maintenance-mode.html>
- Macromedia Flash 8 documentation and release-era references:
  - <https://www.adobe.com/support/documentation/en/flash/fl8/fl_documentation_resources.htm>
  - <https://www.macworld.com/article/177428/flash8.html>
