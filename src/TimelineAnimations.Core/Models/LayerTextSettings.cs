namespace TimelineAnimations.Core.Models;

public sealed class LayerTextSettings
{
    public FlashTextFieldKind FieldKind { get; set; } = FlashTextFieldKind.Static;

    public FlashTextLineMode LineMode { get; set; } = FlashTextLineMode.SingleLine;

    public string FontFamily { get; set; } = string.Empty;

    public bool IsBold { get; set; }

    public bool IsItalic { get; set; }

    public LayerTextAlignment Alignment { get; set; } = LayerTextAlignment.Left;

    public double LetterSpacing { get; set; }

    public double LineHeight { get; set; } = 0d;

    public bool IsSelectable { get; set; } = true;

    public bool ShowBorder { get; set; }

    public string VariableName { get; set; } = string.Empty;

    public int MaxCharacters { get; set; }

    public bool IsPassword { get; set; }

    public bool UseDeviceFonts { get; set; }

    public FlashTextAntiAliasMode AntiAliasMode { get; set; } = FlashTextAntiAliasMode.Animation;

    public bool RenderAsHtml { get; set; }

    public LayerTextSettings Clone()
    {
        return new LayerTextSettings
        {
            FieldKind = FieldKind,
            LineMode = LineMode,
            FontFamily = FontFamily,
            IsBold = IsBold,
            IsItalic = IsItalic,
            Alignment = Alignment,
            LetterSpacing = LetterSpacing,
            LineHeight = LineHeight,
            IsSelectable = IsSelectable,
            ShowBorder = ShowBorder,
            VariableName = VariableName,
            MaxCharacters = MaxCharacters,
            IsPassword = IsPassword,
            UseDeviceFonts = UseDeviceFonts,
            AntiAliasMode = AntiAliasMode,
            RenderAsHtml = RenderAsHtml
        };
    }
}
