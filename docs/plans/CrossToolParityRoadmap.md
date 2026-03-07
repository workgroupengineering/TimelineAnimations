# Cross-Tool Parity Roadmap

## Goal

Close the remaining workflow gaps between `TimelineAnimations Studio` and the parts of:

- Adobe Animate / Macromedia Flash that matter for authoring-rich timelines
- Adobe Premiere Pro that matter for editorial review, range control, and sequence finishing
- Microsoft Expression Blend that matter for XAML state authoring and visual-state preview

This is not a claim that one pass can replicate the entire product surface of three mature tools. It is a parity roadmap that defines the remaining gaps precisely and lands them in phased, testable slices.

Roadmap status:
- completed
- no numbered phases remain open in this roadmap

## External Reference Baseline

The parity targets below were reviewed against current official product guidance:

- Adobe Animate timeline and layer workflows: [helpx.adobe.com/animate/using/timeline-layers.html](https://helpx.adobe.com/animate/using/timeline-layers.html)
- Adobe Animate motion and tween authoring: [helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html](https://helpx.adobe.com/animate/using/creating_a_motion_tween_animation.html)
- Adobe Premiere Pro markers: [helpx.adobe.com/premiere-pro/using/markers.html](https://helpx.adobe.com/premiere-pro/using/markers.html)
- Adobe Premiere Pro source and program monitor workflows: [helpx.adobe.com/premiere-pro/using/source-monitor-program-monitor.html](https://helpx.adobe.com/premiere-pro/using/source-monitor-program-monitor.html)
- Adobe Premiere Pro transitions: [helpx.adobe.com/premiere-pro/using/applying-removing-transitions.html](https://helpx.adobe.com/premiere-pro/using/applying-removing-transitions.html)
- Microsoft XAML visual states and `VisualStateManager`: [learn.microsoft.com/dotnet/api/system.windows.visualstatemanager](https://learn.microsoft.com/dotnet/api/system.windows.visualstatemanager)
- Microsoft storyboards and XAML animation authoring: [learn.microsoft.com/dotnet/desktop/wpf/graphics-multimedia/storyboards-overview](https://learn.microsoft.com/dotnet/desktop/wpf/graphics-multimedia/storyboards-overview)

## Current Product Position

The editor already has strong parity with the core Flash/Animate authoring layer:

- scene-based authoring
- symbol/component library
- nested symbol editing
- frame spans and property tracks
- easing, morphing, and compositing
- media clips
- prototype behaviors and action scripting
- publish/export workflows
- XAML / HTML / SVG animation interchange

The roadmap work defined here is complete. The editor now covers the higher-end editorial, monitor, docking, hierarchy, audio, and state-authoring layers that were previously tracked as open parity gaps.

## Gap Analysis Matrix

| Area | Animate / Flash parity gap | Premiere parity gap | Blend parity gap | Current status | Planned action |
| --- | --- | --- | --- | --- | --- |
| Timeline metadata | labels only, no richer markers/comments/cues | missing chapter/cue/comment markers | n/a | completed | typed scene markers with notes, colors, and timeline rendering landed |
| Playback ranges | basic playhead only | missing in/out and work-area workflow | n/a | completed | in, out, work area, and range-aware playback landed |
| Editorial preview | stage preview only | no source/program monitor split | limited preview surfaces | completed | source/program monitor workspace landed |
| Sequence transitions | no scene-to-scene transitions | no editorial transition layer | n/a | completed | scene transitions and render/export support landed |
| State authoring | button states only | n/a | missing visual state groups and state capture/apply workflow | completed | Blend-style visual state groups and state capture/apply landed |
| Object authoring hierarchy | layer list only | track organization is light | missing objects/state-oriented authoring feel | completed | folders, hierarchy flattening, and Blend-like objects timeline landed |
| Editorial operations | simple range copy/paste | no slip/slide/ripple/razor tools | n/a | completed | razor, slip, slide, ripple, and edit-multiple-frames landed |
| Docking workspace | custom docking shell only | panels are good enough but not Dock-backed | lacks full Blend docking semantics | completed | Dock-for-Avalonia workspace host and persisted layout landed |
| Audio finishing | clip import only | no mixer/meters/track routing | n/a | completed | solo/mute, gain/pan/fades, and audio metering landed |
| State-driven runtime preview | prototype actions exist | n/a | state transitions not first-class | completed | visual-state runtime preview and trigger integration landed |

## Implementation Phases

## Phase 17. Editorial Timeline And Monitor Parity

Status:
- completed

Purpose:
- cover the workflow layer shared by Animate and Premiere: markers, ranges, monitors, and transitions

Deliver:
- typed scene markers: `Cue`, `Comment`, `Chapter`, `Beat`, `PublishNote`
- scene `In`, `Out`, and `Work Area` ranges
- range-aware playback from work area
- source/program monitor strip in the main editor
- scene transitions for all-scenes playback and export

Acceptance:
- the frame timeline renders more than labels
- a user can mark editorial cues without abusing frame labels
- the editor can preview the current stage and an isolated source side by side
- play-all-scenes preview/export respects scene transitions

## Phase 18. Blend-Style Visual State Authoring

Status:
- completed

Purpose:
- bring XAML-state authoring into the editor for Avalonia controls and reusable visuals

Deliver:
- visual state groups per layer
- named states with captured layer templates
- authoring commands to capture, update, apply, and remove states
- state inspector with current group/state selection

Acceptance:
- a selected visual can store multiple named states
- the user can apply saved states during authoring without rebuilding the layer manually
- Avalonia control visuals can be authored with a Blend-like state workflow

## Phase 19. Hierarchy And Power Editing

Status:
- completed

Deliver:
- layer folders and nested hierarchy
- edit-multiple-frames workflow
- frame and layer context menus
- clipboard and duplicate improvements
- object hierarchy closer to Blend `Objects and Timeline`

Implemented:
- folder layers with parent/child hierarchy and collapse state
- hierarchy-aware project, frame, and property track rows
- edit-multiple-frames authoring for inspector and timeline updates
- layer and frame context menus for hierarchy and editorial actions
- hierarchy summaries and folder actions in the inspector

## Phase 20. Editorial Finishing

Status:
- completed

Deliver:
- razor / split, slip, slide, and ripple editing tools
- richer transition catalog
- track solo / mute / lock semantics
- audio meters and simple mix controls

Implemented:
- razor, slip, slide, and ripple commands on the selected media/timeline range
- scene and clip finishing controls alongside the existing transition workflow
- layer solo, mute, and lock-aware rendering/editing semantics
- gain, pan, fade-in, fade-out, and live audio meter summaries in the inspector

## Phase 21. Workspace And Runtime Completion

Status:
- completed

Deliver:
- Dock-for-Avalonia-backed panel model and persisted layouts
- richer monitor docking
- runtime state-trigger integration
- leak/headless coverage for long-lived preview/editor surfaces

Implemented:
- Dock-for-Avalonia workspace host with project, stage, timeline, inspector, and output tools
- persisted dock layouts with reset support
- docked monitor/stage workflow through the document surface
- runtime visual-state trigger integration for prototype behaviors and action scripts
- regression coverage for hierarchy/audio/document round-trip, dock workspace initialization, and headless dock rendering

## Completion Summary

The roadmap is complete. The implementation across phases 17-21 closed the remaining gaps that were still open after the earlier Animate/Blend/Premiere parity work:

1. layer folders, hierarchy flattening, and Blend-style object hierarchy
2. edit-multiple-frames and timeline power-editing workflows
3. razor, slip, slide, and ripple finishing tools
4. track mute/solo plus audio finishing controls and meter summaries
5. Dock-for-Avalonia-backed workspace hosting, persisted layouts, and runtime state triggers

Implemented:

- folder layers, parent/child hierarchy, and hierarchy-aware layer/timeline rows
- inspector-driven edit-multiple-frames authoring
- context-menu and toolbar editorial operations for razor, slip, slide, and ripple
- track mute/solo semantics plus gain, pan, fade, and audio meter summaries
- Dock-for-Avalonia workspace host, resettable layouts, persisted panel arrangement, and runtime visual-state triggers

## Quality Gates

- `dotnet build TimelineAnimations.sln`
- `dotnet test TimelineAnimations.sln`
- document serialization round-trips new marker, transition, and state data
- playback and export still work for legacy documents without the new metadata
- hierarchy, audio, and dock workspace state round-trip correctly
- dock workspace renders under Avalonia headless coverage
