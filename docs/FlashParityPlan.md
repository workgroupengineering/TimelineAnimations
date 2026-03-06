# Flash Parity Roadmap

## Goal

Bring the current Avalonia animation editor toward the major authoring capabilities that made Macromedia Flash compelling:

- reusable assets and symbols
- scene-based authoring
- frame and layer centric timeline editing
- vector drawing and shape tweening
- motion tweening with paths, easing, and onion-skinning
- masks, guide layers, and nested animation
- audio, video, and interactive behaviors
- export and publish workflows

This is not a goal to recreate SWF or the exact Flash runtime. The goal is a modern Avalonia desktop editor that reaches practical parity with the authoring workflow categories that mattered to Flash users.

## Current Baseline

The app already has:

- a WYSIWYG stage
- rectangle, ellipse, text, and path/vector layers
- keyframe tracks for transform and opacity
- direct canvas manipulation
- playback, easing, custom bezier curves, and undo/redo
- multi-layer timeline rows
- reusable symbols and a library panel
- scene-based authoring
- frame spans, blank keys, frame labels, and onion skin
- vector drawing tools with editable path points
- visible motion paths and path morph keyframes
- PNG frame and sequence export

The app does not yet have:

- shape tweens and morph editing
- masks, guide layers, or cameras
- audio and video timeline tracks
- scripting, components, or publish targets beyond image export

## Flash Feature Inventory

### 1. Document And Publishing

- stage size, frame rate, background, publish profile
- scenes
- document metadata and publish settings
- export to runtime formats

### 2. Library And Symbols

- library panel with folders and search
- graphic, movie clip, and button symbols
- reusable symbol instances on stage
- convert selection to symbol
- edit symbol in isolation
- linkage and asset naming
- import bitmap, audio, video, and vector assets

### 3. Timeline And Layering

- frame-based timeline with spans
- keyframes, blank keyframes, frame holds
- multiple layers and folders
- layer visibility, locking, outlines
- guide layers and mask layers
- frame labels and markers
- onion skin and edit multiple frames

### 4. Animation Systems

- classic motion tweens
- motion paths and bezier editing
- shape tweens
- easing presets and custom easing curves
- nested animation via movie clips
- per-symbol playback control

### 5. Drawing And Art Tools

- selection, subselection, free transform
- pen, pencil, brush, line, rectangle, oval, text
- fills, strokes, gradients, swatches
- path/node editing
- shape merge and object drawing modes
- align, distribute, and snap tools

### 6. Effects And Compositing

- masks
- blend modes
- filters such as blur, glow, drop shadow, bevel
- color effects and alpha
- camera/parallax style workflows

### 7. Media

- sound import and timeline synchronization
- event/start/stream sound modes
- waveform preview and trimming
- embedded and linked video

### 8. Interactivity

- buttons and stateful symbols
- component instances
- script/action authoring
- events tied to timeline and stage objects

### 9. Workflow Tooling

- property inspector
- transform/alignment panels
- library management
- test movie / preview
- asset relinking
- shortcuts, context menus, and power-user editing flows

## Product Translation For This App

Some Flash features map directly, others need a modern reinterpretation:

- `SWF runtime` becomes export/playback profiles and a local preview runner.
- `ActionScript timelines` become a modern behavior/event graph or C# plugin surface.
- `Shape merge mode` becomes vector boolean and path-edit workflows.
- `Movie clip symbols` become nested timelines with their own playback contexts.

## Architecture Required For Parity

### Core Document Model

Add these concepts to the core model:

- `SceneModel`
- `LibraryItem`
- `SymbolDefinition`
- `SymbolInstance`
- `FrameSpan`
- `LayerType` with `Normal`, `Mask`, `Guide`, `Folder`, `Audio`, `Video`, `Camera`
- `VectorShape`
- `PathGeometry` and editable nodes
- `TweenSegment` with motion and shape tween variants
- `EffectStack`
- `AudioClipReference` and `VideoClipReference`
- `PublishProfile`

### Editor State

The app needs dedicated editors for:

- stage mode versus symbol isolation mode
- frame selection versus range selection
- drawing tool mode versus transform tool mode
- onion skin preview state
- scene navigation
- library browsing and asset placement

### Rendering

The render pipeline must support:

- nested timelines
- masks and guide layers
- filter stack rendering
- vector path rendering
- audio/video timeline sync

## Milestone Plan

### Phase 1. Library And Symbol Foundations

Status:
- completed

Purpose:
- unlock reusable assets and the first real Flash-style authoring primitive

Deliver:
- document-level library
- symbol definitions
- graphic/movie clip/button symbol kinds
- convert selection to symbol
- place symbol instances on stage
- symbol instance linkage and synchronization
- library panel UI

Acceptance:
- a user can create a symbol from a stage object
- the symbol appears in a library panel
- multiple instances can be placed on stage
- updating the symbol updates linked instances

### Phase 2. Scenes And Document Navigation

Status:
- completed

Purpose:
- move from a single composition to Flash-style document authoring

Deliver:
- scene list and scene navigation
- per-scene stage contents
- add, duplicate, rename, reorder scenes
- scene thumbnails

Acceptance:
- a document can contain multiple scenes
- playback/export can target a scene or the full document

### Phase 3. Frame Timeline And Cel Workflow

Status:
- completed

Purpose:
- add the frame-driven timeline model Flash users expect

Deliver:
- frame grid with spans
- blank keyframe, keyframe, and frame hold editing
- frame labels
- layer durations
- frame range selection and copy/paste
- onion skin

Acceptance:
- a user can create frame-by-frame animation without relying only on numeric tracks

### Phase 4. Symbol Editing And Nested Timelines

Status:
- completed

Purpose:
- enable movie clip and button authoring

Deliver:
- edit-in-place or isolation mode for symbols
- nested timeline playback
- per-instance playback behavior
- basic button state editing

Acceptance:
- a symbol can contain its own layers and timeline
- a movie clip instance plays independently on the stage

### Phase 5. Vector Drawing Toolset

Status:
- completed

Purpose:
- close the largest authoring gap with Flash

Deliver:
- path-based vector layers
- pen, line, brush, rectangle, ellipse, and text tools
- fills, strokes, gradients
- node editing and path transforms
- align/distribute/snap enhancements

Acceptance:
- a user can draw and edit custom vector artwork without leaving the app

### Phase 6. Advanced Tweening

Status:
- completed

Purpose:
- match Flash’s animation workflows beyond transform keyframes

Deliver:
- motion paths
- custom easing graph editor
- shape tweening
- rotation direction controls
- per-property tween spans

Acceptance:
- a user can author both motion and shape tweens visually

### Phase 7. Compositing Features

Status:
- completed

Purpose:
- match stage compositing and animation layout workflows

Deliver:
- mask layers
- guide layers
- blend modes
- filter/effect stack
- camera layer

Acceptance:
- layers can be masked and styled with live effects

### Phase 8. Media Timeline

Status:
- completed

Purpose:
- make the tool practical for motion design and interactive clips

Deliver:
- audio tracks with waveform preview
- start/event/stream playback modes
- video placement and timeline sync
- import pipeline for media assets

Acceptance:
- users can align animation to sound and place video on stage

### Phase 9. Interactivity And Components

Status:
- completed

Purpose:
- cover the interactive-authoring side of Flash

Deliver:
- button symbol states
- behaviors/events panel
- lightweight scripted actions
- component library

Acceptance:
- users can build clickable prototypes and timeline-driven interactions

### Phase 10. Publish And Preview System

Status:
- completed

Purpose:
- finish the document lifecycle

Deliver:
- preview player
- export profiles
- animation package export
- sprite sheet, GIF, MP4, PNG sequence, JSON scene graph
- validation checks before publish

Acceptance:
- a document can be previewed and exported through named publish profiles

### Phase 11. Actions And Runtime Scripting

Status:
- completed

Purpose:
- close the remaining Flash-era authoring gap around frame actions and scripted interactivity

Deliver:
- frame-label action scripts
- behavior-level script blocks
- lightweight action parser and runtime interpreter
- validation for scripted scene, label, layer, and button targets

Acceptance:
- users can author Flash-style action blocks on cues and interactive elements, then test them live in prototype mode

## Implementation Order

The order is driven by dependency, not by visual appeal:

1. Library and symbol foundations
2. Scenes
3. Frame timeline model
4. Symbol isolation and nested playback
5. Vector drawing
6. Advanced tween systems
7. Mask/filter/compositing features
8. Audio/video
9. Interactivity/components
10. Publish system

## Immediate Execution Plan

The first implementation slice should be:

1. Add `LibraryItem` and `SymbolKind` to the core document.
2. Link stage layers to optional symbol/library definitions.
3. Add symbol commands to convert a selected layer into a reusable library asset.
4. Add a library panel to browse and place symbol instances.
5. Synchronize linked instances when the source symbol changes.
6. Add tests for serialization and symbol-instance synchronization.

## Risks

- nested symbols and scenes will significantly expand editor state complexity
- vector drawing requires a different data model than the current primitive layers
- audio/video sync will need a stronger playback clock and media pipeline
- script/behavior support can easily become a runtime product on its own

## Quality Gates Per Phase

- `dotnet build TimelineAnimations.sln` stays green
- logic-heavy features get unit tests
- each phase lands as a usable end-to-end workflow, not just raw models
- UI additions preserve the app’s visual identity and direct-manipulation focus
