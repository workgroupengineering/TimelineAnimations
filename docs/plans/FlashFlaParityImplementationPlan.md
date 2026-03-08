# Flash FLA Support Implementation Plan

## Objective

Complete modern Adobe Animate `FLA` support by treating `FLA` as a compressed `XFL`
package and adding true uncompressed `XFL` folder workflows.

## Phase 1: Dedicated package library

Status: `completed`

Create a new project:

- `src/TimelineAnimations.FlashFormats`

Responsibilities:

- package/container detection
- archive read/write
- XFL folder read/write
- legacy binary FLA detection
- conversion helpers between archive and folder forms

Deliverables:

- `FlashAuthoringContainerKind`
- `FlashAuthoringPackage`
- `FlashAuthoringPackageEntry`
- `FlashAuthoringPackageDetectionResult`
- `FlashAuthoringPackageService`

## Phase 2: Core workflow integration

Status: `completed`

Integrate the new library into `TimelineAnimations.Core`.

Deliverables:

- route `.fla` through archive package handling
- route `.xfl` through:
  - backward-compatible single-XML import
  - true XFL folder import/export APIs
- explicit failure for likely legacy binary FLA with actionable diagnostics

## Phase 3: App workflow and conversion UX

Status: `completed`

Expose the real format workflows in the app.

Deliverables:

- `Open Flash XFL Folder...`
- `Save Flash XFL Folder...`
- `Convert Flash FLA -> XFL Folder...`
- `Convert Flash XFL Folder -> FLA...`
- native menu integration
- fallback menu integration
- dock workspace file actions

## Phase 4: Validation and compatibility reporting

Status: `completed`

Deliverables:

- unit tests for package detection and conversion
- load/save tests for archive and folder forms
- explicit tests for legacy binary FLA rejection
- app-level command coverage
- build and smoke validation

## Scheduling

1. Phase 1: package library
2. Phase 2: core file workflow
3. Phase 3: app integration and commands
4. Phase 4: tests and validation

## Completion criteria

We consider this plan complete when:

- modern `.fla` archive open/save works
- modern uncompressed `.xfl` folder open/save works
- conversion works both directions
- editor loads those documents and preserves editing state
- unsupported legacy binary `FLA` is clearly detected and reported
- the new workflow is test-covered and visible in the app menus

## Result

Completed:

- dedicated `TimelineAnimations.FlashFormats` package/container project
- true modern `FLA` archive handling
- true uncompressed `XFL` folder handling
- app menu actions for open/save/convert
- legacy binary `FLA` detection with explicit rejection messaging

Remaining hard limit:

- legacy pre-XFL binary `FLA` is still not editable from this app because Adobe does not
  publish a complete public authoring specification for that format family
