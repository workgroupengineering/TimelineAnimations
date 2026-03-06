# SVG SMIL Interop Plan

## Objective

Support a vector-friendly interchange format that preserves stage geometry and timeline playback in a standards-based file.

## Supported SVG Surface

- `svg`
- `defs`
- `linearGradient`
- `rect`
- `ellipse`
- `text`
- `path`
- `g`
- `animate`
- `animateTransform`

## Import Strategy

1. Parse SVG XML.
2. Read stage size from `width`, `height`, or `viewBox`.
3. Convert shapes into timeline layers.
4. Convert metadata-tagged control groups into `AvaloniaControl` layers.
5. Parse SMIL animations into keyframes.
6. Parse path `d` animation values into shape keyframes when the command set is supported.

## Export Strategy

1. Export active scene as one SVG.
2. Map shape layers directly to SVG geometry.
3. Approximate Avalonia controls as SVG groups with metadata.
4. Emit baked `animate` and `animateTransform` blocks.
5. Emit path morph values for supported path layers.

## Acceptance

- exported SVG opens in standard SVG viewers
- supported layers re-import into equivalent timelines
- path layers preserve static geometry and supported morph keys
