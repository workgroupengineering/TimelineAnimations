# Post Parity Roadmap

## Goal

Extend the completed Flash-parity editor into a production-grade animation studio with deeper asset management, faster power-user editing, stronger vector authoring, tighter workspace ergonomics, and hardened runtime/export behavior.

This roadmap starts after Flash parity Phase 11 and focuses on the remaining gaps that matter once the core authoring model already exists.

## Current Extension Baseline

The editor already has:

- symbol and component libraries
- scene and symbol isolation editing
- frame spans, property tracks, easing, morph keys, and export
- compositing, media, prototype interactivity, publish profiles, and action scripting

The next work is not about missing the basics. It is about making the existing toolchain deeper, faster, and more resilient.

## Phase 12. Library Management Depth

Status:
- completed

Purpose:
- turn the library from a flat symbol list into a real asset-management surface

Deliver:
- folder paths for library items and component assets
- library search and folder filtering
- explicit linkage identifiers separate from display names
- relink workflow for swapping a stage instance to another library item
- stronger rename and metadata editing with uniqueness rules
- source-path metadata for imported or externally tracked assets

Acceptance:
- users can organize the library by folders
- users can search by asset name, folder, linkage id, or source path
- renaming a symbol does not destroy its linkage id
- a selected stage instance can be relinked to another library asset

Implementation slices:
1. Add library metadata model and normalization rules.
2. Add filter/search state and metadata editors in the library dock.
3. Add stage-instance relink command and tests.

## Phase 13. Timeline Ergonomics And Power Editing

Status:
- planned

Purpose:
- make the timeline feel fast under heavy editorial use

Deliver:
- layer folders with collapse/expand and folder operations
- outline mode for shape-heavy scenes
- edit-multiple-frames workflow with visible frame range state
- richer markers for labels, cues, comments, and publish notes
- frame and layer context menus
- stronger clipboard, duplicate, insert, and range commands

Acceptance:
- users can manage large timelines without flattening all layers
- users can edit multiple frames intentionally and see that state clearly
- timeline markers can communicate more than frame labels alone

Dependencies:
- no blocking core dependency beyond current frame timeline

## Phase 14. Vector Authoring Deepening

Status:
- planned

Purpose:
- close the gap between basic path editing and illustration-grade vector authoring

Deliver:
- boolean operations: union, subtract, intersect, exclude
- object-drawing versus merge-style drawing modes
- distribute and spacing tools beyond basic align
- stronger point editing: corner/smooth conversion, segment insertion, segment deletion
- better fill and stroke handling when shapes merge

Acceptance:
- users can build and edit compound artwork without leaving the editor
- object mode and merge mode behave differently and predictably

Dependencies:
- path model and vector editing infrastructure already exists

## Phase 15. Studio Workflow Polish

Status:
- in progress

Purpose:
- make the editor behave like a daily-use studio app instead of a feature demo

Deliver:
- keyboard shortcut map for high-frequency commands
- context menus across stage, library, timeline, and layer list
- rulers, guides, and guide snapping
- dock and panel persistence
- Flash-like panel behavior for collapse, focus, and restore
- better status and hint surfaces for discoverability

Progress:
- compact workspace density
- docked, overlay, and hidden panel modes for the main docks
- workspace presets for stage-first and timeline-first editing
- persisted workspace layout with direct panel restore and collapse controls
- collapsed edge handles and focus actions for stage, timeline, tools, and inspector workflows
- local dock chrome controls for dock, overlay, and hide state changes
- keyboard shortcuts and shortcut hints for workspace focus and panel toggling
- workspace context menus on the strip and dock headers

Acceptance:
- the app can be shaped into a preferred workspace and restores it
- common actions are reachable without hunting for toolbar buttons

Dependencies:
- compact shell and splitter-based layout already exists

## Phase 16. Runtime And Export Hardening

Status:
- planned

Purpose:
- make preview and publishing stable on larger projects

Deliver:
- stronger playback clocking for mixed media documents
- performance passes for larger scene graphs and deeper libraries
- more headless UI coverage for critical editor surfaces
- leak tests around long-lived windows, controls, timers, and preview flows
- plugin/runtime extension surface for richer interactive behaviors

Acceptance:
- larger projects preview and export more reliably
- leak and headless regressions are caught by automated tests

Dependencies:
- existing preview/export/runtime system

## Execution Order

1. Phase 12: Library Management Depth
2. Phase 13: Timeline Ergonomics And Power Editing
3. Phase 14: Vector Authoring Deepening
4. Phase 15: Studio Workflow Polish
5. Phase 16: Runtime And Export Hardening

## Immediate Next Slice

Phase 13 is next. Phase 12 is now landed and provides the asset-management base for the remaining work:

1. add layer folders with collapse and visibility inheritance
2. add edit-multiple-frames mode with visible range state
3. add richer frame markers and cue/comment metadata
4. add layer and frame context menus for power editing

## Quality Gates

- `dotnet build TimelineAnimations.sln` stays green
- `dotnet test TimelineAnimations.sln` stays green
- document serialization keeps round-tripping new metadata
- every phase lands with a visible end-to-end workflow, not only backend types
