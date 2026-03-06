# Avalonia Toolbox Plan

## Objective

Turn the current art palette into a practical Avalonia authoring toolbox for control-heavy UI animation work.

## Toolbox Groups

### Shapes

- Rectangle
- Ellipse
- Text
- Path

### Visuals

- Border / Card
- Panel
- Image Placeholder

### Controls

- Button
- TextBox
- CheckBox
- ToggleButton
- Slider
- ProgressBar

## Editing Requirements

- drag and drop to stage
- sensible default sizes and content
- stage rendering that resembles the target Avalonia control
- inspector support for:
  - control type
  - content text
  - checked state
  - value/min/max
  - image source label

## Acceptance

- toolbox items are visible in the Assets tab
- dropping a toolbox item creates an `AvaloniaControl` layer
- exported Avalonia XAML uses real Avalonia controls for those layers
