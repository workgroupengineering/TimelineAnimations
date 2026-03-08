using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

public partial class AnimationExchangeCodeEditorControl : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<AnimationExchangeCodeEditorControl, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<AnimationExchangeFormat> FormatProperty =
        AvaloniaProperty.Register<AnimationExchangeCodeEditorControl, AnimationExchangeFormat>(
            nameof(Format),
            AnimationExchangeFormat.AvaloniaXaml);

    private readonly RegistryOptions _registryOptions = new(ThemeName.DarkPlus);
    private TextMate.Installation? _textMateInstallation;
    private TextEditor? _editor;

    public AnimationExchangeCodeEditorControl()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor");
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public AnimationExchangeFormat Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureTextMateInstalled();
        ApplyEditorText();
        ApplyGrammar();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _textMateInstallation?.Dispose();
        _textMateInstallation = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            ApplyEditorText();
            return;
        }

        if (change.Property == FormatProperty)
        {
            ApplyGrammar();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EnsureTextMateInstalled()
    {
        if (_editor is null || _textMateInstallation is not null)
        {
            return;
        }

        _textMateInstallation = _editor.InstallTextMate(_registryOptions);
    }

    private void ApplyEditorText()
    {
        if (_editor is null)
        {
            return;
        }

        var nextText = Text ?? string.Empty;
        if (string.Equals(_editor.Text, nextText, StringComparison.Ordinal))
        {
            return;
        }

        _editor.Text = nextText;
    }

    private void ApplyGrammar()
    {
        if (_textMateInstallation is null)
        {
            return;
        }

        var extension = Format == AnimationExchangeFormat.HtmlCss ? ".html" : ".xml";
        var scopeName = _registryOptions.GetScopeByExtension(extension)
            ?? _registryOptions.GetScopeByLanguageId(extension == ".html" ? "html" : "xml");
        if (!string.IsNullOrWhiteSpace(scopeName))
        {
            _textMateInstallation.SetGrammar(scopeName);
        }
    }
}
