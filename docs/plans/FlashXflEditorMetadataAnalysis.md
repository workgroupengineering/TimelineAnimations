# Flash XFL Editor Metadata Analysis

## Status

Planned on March 6, 2026.

## Baseline

Flash XFL interop already preserves:

- scenes, layers, folders, labels, markers, and transitions
- symbols, button states, instance names, linkage, Scale-9, registration points
- guide bindings and guide options
- explicit motion tracks, easing, and shape tweens
- compositing, media sync, and packaged `.fla` archives
- static / dynamic / input text fields with line mode, variable name, selectable, and border metadata

## Remaining high-value parity gaps

The remaining gaps are mostly editor-facing authoring controls that still exist in
Adobe Animate’s Properties and Timeline workflows but are not yet modeled in this app.

| Area | Current state | Remaining gap |
| --- | --- | --- |
| Layer authoring display metadata | Visibility, lock, mute/solo, mask/guide/camera roles | No outline mode and no layer outline color that survive XFL round-trip. |
| Symbol instance performance metadata | Instance naming and playback settings are preserved | No cache-as-bitmap metadata or cache background color on instances. |
| Classic text field behavior | Field kind, line mode, HTML, variable, border, selectable are preserved | No max character limit, password mode, device-font toggle, or anti-alias mode metadata. |

## Adobe references used

- [Timeline and layers in Animate](https://helpx.adobe.com/animate/using/timeline-layers.html)
- [Using symbol instances in Animate](https://helpx.adobe.com/id_id/animate/using/symbol-instances.html)
- [Scaling and caching symbols on the Stage](https://helpx.adobe.com/sg/animate/using/scaling-caching-symbols.html)
- [Classic text in Animate](https://helpx.adobe.com/si/animate/using/classic-text.html)

## Decision

Implement one more XFL parity pass focused on editor metadata:

1. layer outline/color metadata
2. symbol instance cache metadata
3. richer classic text field constraints and rendering metadata

## Why this slice matters

- It closes some of the last visible gaps between imported Animate authoring data and the app’s inspector.
- It improves round-trip fidelity for UI-heavy Flash projects that rely on input fields and symbol authoring settings.
- It stays aligned with the current app architecture because the missing features are metadata and editor-surface behaviors, not a new runtime model.
