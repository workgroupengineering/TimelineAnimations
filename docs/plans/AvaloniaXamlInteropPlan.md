# Avalonia XAML Interop Plan

## Objective

Support importing and exporting Avalonia keyframe-based animations from XAML using Avalonia 11 style animation syntax.

## Supported Visual Types

- `Rectangle`
- `Ellipse`
- `Path`
- `TextBlock`
- `Border`
- `Button`
- `TextBox`
- `CheckBox`
- `ToggleButton`
- `Slider`
- `ProgressBar`
- `Image`

## Supported Animated Properties

- `Canvas.Left`
- `Canvas.Top`
- `Width`
- `Height`
- `Opacity`
- `RenderTransform.Angle`
- `Data` for `Path` when the path command structure is compatible

## Import Strategy

1. Parse XAML with `XDocument`.
2. Find the supported visual root and collect named children.
3. Map element properties into `TimelineLayer` defaults/style/control settings.
4. Resolve `Style Selector="#Name"` animations.
5. Convert `KeyFrame` setter values to internal keyframes.
6. Convert `Path.Data` keyframes into shape keyframes.
7. Build a single-scene `TimelineDocument`.

## Export Strategy

1. Snapshot the active scene.
2. Emit a `UserControl` containing a `Canvas`.
3. Emit one named visual element per layer.
4. Emit selector-based animations in `UserControl.Styles`.
5. Bake per-segment easing into explicit samples where needed.

## Acceptance

- exported XAML opens as valid Avalonia markup
- supported controls remain real Avalonia controls
- import reconstructs the same stage size and supported keyframes
