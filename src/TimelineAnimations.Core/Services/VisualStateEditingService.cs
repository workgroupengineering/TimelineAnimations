using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class VisualStateEditingService
{
    public static VisualStateGroupModel EnsureGroup(TimelineLayer layer, string groupName)
    {
        var normalizedGroupName = string.IsNullOrWhiteSpace(groupName) ? "CommonStates" : groupName.Trim();
        var group = layer.VisualStateGroups.FirstOrDefault(item => string.Equals(item.Name, normalizedGroupName, StringComparison.OrdinalIgnoreCase));
        if (group is not null)
        {
            return group;
        }

        group = new VisualStateGroupModel
        {
            Name = normalizedGroupName
        };
        layer.VisualStateGroups.Add(group);
        return group;
    }

    public static VisualStateModel CaptureState(TimelineLayer layer, string groupName, string stateName, double time)
    {
        var normalizedStateName = string.IsNullOrWhiteSpace(stateName) ? "State" : stateName.Trim();
        var group = EnsureGroup(layer, groupName);
        var state = group.States.FirstOrDefault(item => string.Equals(item.Name, normalizedStateName, StringComparison.OrdinalIgnoreCase));
        if (state is null)
        {
            state = new VisualStateModel
            {
                Name = normalizedStateName
            };
            group.States.Add(state);
        }

        state.Template = CreateTemplateFromSample(layer, TimelineInterpolationService.SampleLayer(layer, time));
        group.ActiveStateName = state.Name;
        return state;
    }

    public static bool RemoveState(TimelineLayer layer, Guid groupId, Guid stateId)
    {
        var group = layer.VisualStateGroups.FirstOrDefault(item => item.Id == groupId);
        if (group is null)
        {
            return false;
        }

        var removedState = group.States.FirstOrDefault(item => item.Id == stateId);
        var removed = group.States.RemoveAll(item => item.Id == stateId) > 0;
        if (!removed)
        {
            return false;
        }

        if (removedState is not null &&
            string.Equals(group.ActiveStateName, removedState.Name, StringComparison.OrdinalIgnoreCase))
        {
            group.ActiveStateName = group.States.FirstOrDefault()?.Name ?? string.Empty;
        }

        if (group.States.Count == 0)
        {
            layer.VisualStateGroups.RemoveAll(item => item.Id == groupId);
        }

        return true;
    }

    public static bool RemoveGroup(TimelineLayer layer, Guid groupId)
    {
        return layer.VisualStateGroups.RemoveAll(item => item.Id == groupId) > 0;
    }

    public static bool ApplyState(TimelineLayer layer, Guid groupId, Guid stateId)
    {
        var group = layer.VisualStateGroups.FirstOrDefault(item => item.Id == groupId);
        var state = group?.States.FirstOrDefault(item => item.Id == stateId);
        if (group is null || state is null)
        {
            return false;
        }

        ApplyTemplate(layer, state.Template);
        group.ActiveStateName = state.Name;
        return true;
    }

    public static bool ApplyState(TimelineLayer layer, string groupName, string stateName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        var group = layer.VisualStateGroups.FirstOrDefault(item => string.Equals(item.Name, groupName.Trim(), StringComparison.OrdinalIgnoreCase));
        var state = group?.States.FirstOrDefault(item => string.Equals(item.Name, stateName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (group is null || state is null)
        {
            return false;
        }

        ApplyTemplate(layer, state.Template);
        group.ActiveStateName = state.Name;
        return true;
    }

    private static TimelineLayer CreateTemplateFromSample(TimelineLayer source, LayerSnapshot snapshot)
    {
        var template = DocumentSerializer.Clone(source);
        template.FrameSpans.Clear();
        template.ShapeKeyframes.Clear();
        template.Tracks.Clear();
        template.Behaviors.Clear();
        template.VisualStateGroups.Clear();
        template.Defaults = new LayerDefaults
        {
            X = snapshot.X,
            Y = snapshot.Y,
            Width = snapshot.Width,
            Height = snapshot.Height,
            Rotation = snapshot.Rotation,
            Opacity = snapshot.Opacity
        };
        template.Style = new LayerStyle
        {
            Fill = snapshot.Fill,
            Stroke = snapshot.Stroke,
            StrokeThickness = snapshot.StrokeThickness,
            Text = snapshot.Text,
            FontSize = snapshot.FontSize,
            TextSettings = snapshot.TextSettings.Clone(),
            CornerRadius = snapshot.CornerRadius,
            UseGradient = snapshot.UseGradient,
            GradientFrom = snapshot.GradientFrom,
            GradientTo = snapshot.GradientTo,
            IsClosed = snapshot.IsClosed,
            PathPoints = VectorPathService.ClonePoints(snapshot.PathPoints),
            AvaloniaControl = snapshot.AvaloniaControl.Clone()
        };
        template.Compositing = snapshot.Compositing.Clone();
        return template;
    }

    private static void ApplyTemplate(TimelineLayer target, TimelineLayer template)
    {
        target.Kind = template.Kind;
        target.Defaults = DocumentSerializer.Clone(template.Defaults);
        target.Style = DocumentSerializer.Clone(template.Style);
        target.Compositing = template.Compositing.Clone();
        target.Media = template.Media.Clone();
        target.SymbolPlaybackMode = template.SymbolPlaybackMode;
        target.SymbolPlaybackOffset = template.SymbolPlaybackOffset;
        target.SymbolLockedFrame = template.SymbolLockedFrame;
        target.SymbolButtonState = template.SymbolButtonState;
    }
}
