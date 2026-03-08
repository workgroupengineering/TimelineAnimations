# Adobe Animate Parity Audit Follow-up Plan

## Goal

Review the recent Adobe Animate parity plans against the current codebase and
close the remaining tasks that are still only partially implemented in practice.

This follow-up is intentionally narrow. It does not invent new parity scope.
It finishes the parts that the previous plans described, but that are still
missing full end-to-end UX or visibility.

## Audit Result

The previous parity plans are broadly implemented, but three tasks still remain
partial:

1. Animate document target conversion is present as direct field mutation, but
   it is still summary-only and does not expose a detailed compatibility issue
   list in the document inspector surfaces.
2. The docked workspace has the stronger Animate-style quick tool/property strip,
   but the classic shell stage surface still lacks that parity.
3. Primitive/authored shape metadata exists, but it is not surfaced clearly in
   the project-layer list, which weakens the “clear authored-shape summaries”
   goal from the effects/shapes/timeline/UX analysis.
4. The docked inspector exposes Animate/FLX 3D document controls, but the
   classic shell document surface was still missing the same `Enable 3D`,
   perspective, and vanishing-point controls.
5. The classic shell project panel still lagged the docked workspace by missing
   the quick `+ Star` primitive insertion action.
6. The docked inspector exposed object-level `Rotation X`, `Rotation Y`, and
   `Z depth`, but the classic shell object surface still only exposed 2D
   transform fields.

## Follow-up Phases

| Phase | Scope | Status |
| --- | --- | --- |
| 38 | Animate target conversion UX and compatibility issue list | completed |
| 39 | Classic-shell tool quick-strip parity | completed |
| 40 | Project-surface authored-shape summaries | completed |
| 41 | Classic-shell Animate document 3D control parity | completed |
| 42 | Classic-shell primitive add parity (`+ Star`) | completed |
| 43 | Classic-shell object 3D transform parity | completed |

## Phase 38. Animate Target Conversion UX

### Gaps being closed

- compatibility warnings are only summarized, not listed
- target conversion is implicit; the user cannot inspect detailed target issues
  in the document inspector before or after conversion

### Deliverables

- add a detailed `AnimateDocumentCompatibilityIssues` surface in the ViewModel
- add `HasAnimateDocumentCompatibilityIssues`
- show the issues list in both docked and classic document inspector surfaces
- make the convert/defaults path explicit in the UI text so it reads as a
  target conversion workflow instead of only generic settings

### Acceptance

- users can see each target-specific compatibility warning directly from the
  document inspector
- compatibility details update immediately when document target settings change

## Phase 39. Classic-Shell Tool Quick-Strip Parity

### Gaps being closed

- the docked stage surface has tool-sensitive quick controls
- the classic shell still exposes only fit/zoom/onion basics in the stage header

### Deliverables

- port the dock-stage quick control strip concepts into the classic shell:
  - shape drawing mode
  - shape primitive quick fields
  - motion preset quick apply/capture/delete
  - active tool option quick controls for zoom/lasso/pencil/eraser/rotate view
- keep the strip compact and consistent with the docked surface

### Acceptance

- the classic stage surface exposes the same practical quick workflows as the
  docked stage surface
- users do not need to leave the stage to reach basic tool-sensitive options

## Phase 40. Project-Surface Authored Shape Summaries

### Gaps being closed

- authored primitive metadata exists in the model and inspector
- project/layer cards still read too generically for shapes

### Deliverables

- add a dedicated shape/authoring summary on `LayerViewModel`
- surface authored primitive details in the project/layer list:
  - drawing mode
  - rectangle corner summary
  - ellipse start/sweep summary
  - poly-star/polygon summary
- keep non-shape layers unchanged

### Acceptance

- shape layers show authored primitive metadata directly in the project panel
- authored shape state is easier to scan without opening the inspector

## Phase 41. Classic-Shell Animate Document 3D Parity

### Gaps being closed

- docked inspector already exposes document 3D target controls
- classic shell document settings still lagged behind that parity

### Deliverables

- add classic-shell controls for:
  - `Enable 3D transforms`
  - perspective angle
  - vanishing point X
  - vanishing point Y

### Acceptance

- classic and docked document surfaces expose the same practical Animate/FLX 3D
  document settings

## Phase 42. Classic-Shell Primitive Add Parity

### Gaps being closed

- docked project surface already includes `+ Star`
- classic shell project panel was still missing the same primitive quick-add

### Deliverables

- add `+ Star` to the classic shell layer/project command row

### Acceptance

- users can add poly-star primitives from both shells without leaving the
  project panel workflow

## Phase 43. Classic-Shell Object 3D Transform Parity

### Gaps being closed

- docked object inspector already exposes `Rotation X`, `Rotation Y`, and `Z depth`
- classic shell object transform section still only exposed 2D transforms

### Deliverables

- add classic shell object controls for:
  - `Rotation X`
  - `Rotation Y`
  - `Z depth`

### Acceptance

- classic and docked object inspector surfaces expose the same practical 3D
  transform authoring controls

## Execution Order

1. Phase 38
2. Phase 39
3. Phase 40
4. Phase 41
5. Phase 42
6. Phase 43

## Validation

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- targeted tests for:
  - Animate target compatibility issue exposure
  - classic-shell quick-strip state
  - layer/project authored-shape summaries
- optional full-suite rerun if the unrelated headless font failures are not in
  the way

## Result

This follow-up pass implemented:

- detailed Animate target compatibility issue lists in docked and classic
  document inspector surfaces
- explicit target conversion messaging in the Animate document workflow
- classic-shell stage quick-strip parity for:
  - drawing mode
  - rectangle corner quick fields
  - ellipse start/sweep
  - poly-star controls
  - motion preset quick actions
  - active tool quick options
- authored primitive shape summaries in project layer cards and inspector
  selection headers
- classic-shell Animate document 3D controls for target perspective and
  vanishing-point settings
- classic-shell `+ Star` primitive insertion
- classic-shell object 3D transform controls

Validation completed:

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`: passed
- targeted tests: `3/3` passing
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`: smoke-launched successfully and was stopped manually
