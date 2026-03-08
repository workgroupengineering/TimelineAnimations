using System.Globalization;
using System.Text.RegularExpressions;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static partial class ComponentParameterService
{
    public static void NormalizeDefinitions(LibraryItem item)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in item.ComponentParameters)
        {
            definition.Name = EnsureUniqueName(definition.Name, usedNames);
            definition.TargetLayerName = definition.TargetLayerName?.Trim() ?? string.Empty;
            definition.DefaultValue ??= string.Empty;
            definition.Description ??= string.Empty;
        }
    }

    public static void EnsureOverrides(TimelineLayer layer, LibraryItem item)
    {
        NormalizeDefinitions(item);
        layer.ComponentParameterOverrides ??= [];

        var existing = layer.ComponentParameterOverrides
            .Where(overrideValue => !string.IsNullOrWhiteSpace(overrideValue.Name))
            .GroupBy(overrideValue => overrideValue.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var ordered = new List<ComponentParameterOverride>(item.ComponentParameters.Count);
        foreach (var definition in item.ComponentParameters)
        {
            if (!existing.TryGetValue(definition.Name, out var overrideValue))
            {
                overrideValue = new ComponentParameterOverride
                {
                    Name = definition.Name,
                    Value = definition.DefaultValue ?? string.Empty
                };
            }

            overrideValue.Name = definition.Name;
            overrideValue.Value ??= definition.DefaultValue ?? string.Empty;
            ordered.Add(overrideValue);
        }

        layer.ComponentParameterOverrides = ordered;
    }

    public static IReadOnlyList<ComponentParameterBinding> BuildBindings(TimelineLayer layer, LibraryItem item)
    {
        EnsureOverrides(layer, item);
        var valueLookup = layer.ComponentParameterOverrides.ToDictionary(
            overrideValue => overrideValue.Name,
            overrideValue => overrideValue.Value ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var bindings = new List<ComponentParameterBinding>(item.ComponentParameters.Count);
        foreach (var definition in item.ComponentParameters)
        {
            valueLookup.TryGetValue(definition.Name, out var overrideValue);
            bindings.Add(new ComponentParameterBinding(
                definition.Name,
                definition.Kind,
                definition.BindingTarget,
                definition.TargetLayerName,
                string.IsNullOrWhiteSpace(overrideValue) ? definition.DefaultValue ?? string.Empty : overrideValue));
        }

        return bindings;
    }

    public static LayerSnapshot ApplyToSnapshot(
        string layerName,
        LayerSnapshot snapshot,
        IReadOnlyList<ComponentParameterBinding> bindings)
    {
        if (bindings.Count == 0)
        {
            return snapshot;
        }

        var next = snapshot;
        var control = snapshot.AvaloniaControl.Clone();
        var textSettings = snapshot.TextSettings.Clone();

        foreach (var binding in bindings)
        {
            if (!string.IsNullOrWhiteSpace(binding.TargetLayerName) &&
                !string.Equals(binding.TargetLayerName, layerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            next = binding.BindingTarget switch
            {
                ComponentParameterBindingTarget.Text => next with { Text = binding.Value },
                ComponentParameterBindingTarget.Content => next with { AvaloniaControl = ApplyControl(control, item => item.Content = binding.Value) },
                ComponentParameterBindingTarget.SecondaryContent => next with { AvaloniaControl = ApplyControl(control, item => item.SecondaryContent = binding.Value) },
                ComponentParameterBindingTarget.Fill => next with { Fill = binding.Value },
                ComponentParameterBindingTarget.Stroke => next with { Stroke = binding.Value },
                ComponentParameterBindingTarget.GradientFrom => next with { GradientFrom = binding.Value },
                ComponentParameterBindingTarget.GradientTo => next with { GradientTo = binding.Value },
                ComponentParameterBindingTarget.Width => TryApplyDouble(next, binding.Value, static (snapshot, value) => snapshot with { Width = Math.Max(1d, value) }),
                ComponentParameterBindingTarget.Height => TryApplyDouble(next, binding.Value, static (snapshot, value) => snapshot with { Height = Math.Max(1d, value) }),
                ComponentParameterBindingTarget.Opacity => TryApplyDouble(next, binding.Value, static (snapshot, value) => snapshot with { Opacity = TimelineMath.Clamp(value, 0d, 1d) }),
                ComponentParameterBindingTarget.FontSize => TryApplyDouble(next, binding.Value, static (snapshot, value) => snapshot with { FontSize = Math.Max(1d, value) }),
                ComponentParameterBindingTarget.CornerRadius => TryApplyDouble(next, binding.Value, static (snapshot, value) => snapshot with { CornerRadius = Math.Max(0d, value) }),
                ComponentParameterBindingTarget.Source => next with { AvaloniaControl = ApplyControl(control, item => item.Source = binding.Value) },
                _ => next
            };

            if (binding.BindingTarget == ComponentParameterBindingTarget.Text)
            {
                textSettings.VariableName = ResolveInlineTokens(textSettings.VariableName, bindings);
                next = next with { TextSettings = textSettings };
            }
        }

        next = next with
        {
            Text = ResolveInlineTokens(next.Text, bindings),
            Fill = ResolveInlineTokens(next.Fill, bindings),
            Stroke = ResolveInlineTokens(next.Stroke, bindings),
            GradientFrom = ResolveInlineTokens(next.GradientFrom, bindings),
            GradientTo = ResolveInlineTokens(next.GradientTo, bindings)
        };

        control.Content = ResolveInlineTokens(control.Content, bindings);
        control.SecondaryContent = ResolveInlineTokens(control.SecondaryContent, bindings);
        control.Source = ResolveInlineTokens(control.Source, bindings);
        textSettings.VariableName = ResolveInlineTokens(textSettings.VariableName, bindings);
        return next with
        {
            AvaloniaControl = control,
            TextSettings = textSettings
        };
    }

    public static IReadOnlyList<ComponentParameterDefinition> CreateSeedDefinitions(TimelineLayer layer)
    {
        var definitions = new List<ComponentParameterDefinition>();
        if (layer.Kind == LayerKind.Text && !string.IsNullOrWhiteSpace(layer.Style.Text))
        {
            definitions.Add(new ComponentParameterDefinition
            {
                Name = "label",
                Kind = ComponentParameterKind.String,
                BindingTarget = ComponentParameterBindingTarget.Text,
                DefaultValue = layer.Style.Text,
                Description = "Primary text content"
            });
        }

        if (layer.Kind == LayerKind.AvaloniaControl && !string.IsNullOrWhiteSpace(layer.Style.AvaloniaControl.Content))
        {
            definitions.Add(new ComponentParameterDefinition
            {
                Name = "content",
                Kind = ComponentParameterKind.String,
                BindingTarget = ComponentParameterBindingTarget.Content,
                DefaultValue = layer.Style.AvaloniaControl.Content,
                Description = "Primary control content"
            });
        }

        if (layer.Style.HasFill)
        {
            definitions.Add(new ComponentParameterDefinition
            {
                Name = "fill",
                Kind = ComponentParameterKind.Color,
                BindingTarget = ComponentParameterBindingTarget.Fill,
                DefaultValue = layer.Style.Fill,
                Description = "Primary fill color"
            });
        }

        return definitions;
    }

    public static string ResolveInlineTokens(string value, IReadOnlyList<ComponentParameterBinding> bindings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return ParameterTokenRegex().Replace(value, match =>
        {
            var name = match.Groups[1].Value.Trim();
            var binding = bindings.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(binding.Name) ? match.Value : binding.Value;
        });
    }

    private static LayerSnapshot TryApplyDouble(LayerSnapshot snapshot, string value, Func<LayerSnapshot, double, LayerSnapshot> apply)
    {
        if (double.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return apply(snapshot, parsed);
        }

        return snapshot;
    }

    private static AvaloniaControlSettings ApplyControl(AvaloniaControlSettings settings, Action<AvaloniaControlSettings> apply)
    {
        apply(settings);
        return settings;
    }

    private static string EnsureUniqueName(string value, HashSet<string> usedNames)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "parameter" : value.Trim();
        var candidate = normalized;
        var suffix = 2;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{normalized}{suffix}";
            suffix++;
        }

        return candidate;
    }

    [GeneratedRegex(@"\{param:([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ParameterTokenRegex();
}

public readonly record struct ComponentParameterBinding(
    string Name,
    ComponentParameterKind Kind,
    ComponentParameterBindingTarget BindingTarget,
    string TargetLayerName,
    string Value);
