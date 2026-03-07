# Flash XFL Visual Effects Parity Analysis

## Status

Implemented on March 7, 2026.

## Baseline

Flash XFL interop already preserves:

- scenes, symbols, library linkage, Scale-9, registration points, and packaged `.fla`
- explicit motion tracks, easing, shape tweens, guide bindings, and transform parity
- text field metadata, layer outline/cache metadata, and media sync
- app-level compositing controls for blur, glow, shadow, tint, brightness, and saturation

The remaining interop gap is that Adobe-authored XFL visual effects are still only partially represented.

## Remaining high-value parity gaps

| Area | Current state | Remaining gap |
| --- | --- | --- |
| Native Flash color effects | The app has generic tint/brightness controls | Imported XFL instance color effects such as `Alpha`, `Tint`, `Brightness`, and `Advanced` channel transforms are not modeled explicitly, so Animate-authored intent can be flattened or lost. |
| Native XFL filters | The app has internal blur/glow/shadow fields | XFL import/export does not yet read or write native Flash filter collections like `BlurFilter`, `GlowFilter`, and `DropShadowFilter`, so external Animate projects lose fidelity. |
| Stage/render parity | Export rendering applies more visual adjustment logic than the direct canvas path | The stage preview should show the same Flash color effects as export so editing remains trustworthy. |
| Inspector parity | The inspector exposes generic compositing fields | There is no explicit Flash-oriented authoring section for color effect modes and advanced channel values. |

## Adobe references used

- [Animate XML / XFL file format](https://helpx.adobe.com/animate/using/extensible-markup-language-file.html)
- [Use the Color panel in Animate](https://helpx.adobe.com/ie/animate/using/working-with-color.html)
- [Apply graphic filters in Animate](https://helpx.adobe.com/ie/animate/using/apply-filters.html)
- [Extending Adobe Animate CC](https://www.adobe.com/content/dam/acom/en/devnet/flash/articles/extending_animate_cc.pdf)

## Decision

Implement one more parity pass focused on visual-effects fidelity:

1. add an explicit Flash color-effect model over the existing compositing system
2. map native XFL color-effect attributes into that model on import/export
3. map native XFL filter collections into the existing blur/glow/shadow model
4. apply the same color-effect logic in stage preview and export rendering
5. expose Flash color-effect authoring in the inspector and summaries

## Why this slice matters

- It improves interoperability with real Adobe-authored XFL instead of only app-generated XFL.
- It closes a rendering trust gap between imported data, stage preview, and exported output.
- It preserves an Adobe Animate workflow area that designers actually use on symbol instances and text.
