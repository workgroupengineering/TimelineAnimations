using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Loader;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using TimelineAnimations.App.Services;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Controls;

public partial class AnimationExchangeVisualPreviewControl : UserControl
{
    public static readonly StyledProperty<AnimationExchangeFormat> FormatProperty =
        AvaloniaProperty.Register<AnimationExchangeVisualPreviewControl, AnimationExchangeFormat>(
            nameof(Format),
            AnimationExchangeFormat.AvaloniaXaml);

    public static readonly StyledProperty<string> PreviewTextProperty =
        AvaloniaProperty.Register<AnimationExchangeVisualPreviewControl, string>(nameof(PreviewText), string.Empty);

    public static readonly StyledProperty<TimelineDocument?> PreviewDocumentProperty =
        AvaloniaProperty.Register<AnimationExchangeVisualPreviewControl, TimelineDocument?>(nameof(PreviewDocument));

    public static readonly StyledProperty<double> PreviewTimeProperty =
        AvaloniaProperty.Register<AnimationExchangeVisualPreviewControl, double>(nameof(PreviewTime));

    public static readonly StyledProperty<string> PreviewSummaryProperty =
        AvaloniaProperty.Register<AnimationExchangeVisualPreviewControl, string>(nameof(PreviewSummary), string.Empty);

    private ContentPresenter? _runtimePreviewHost;
    private Viewbox? _runtimeViewbox;
    private Image? _bitmapPreviewImage;
    private Border? _emptyStateBorder;
    private TextBlock? _emptyStateTitle;
    private TextBlock? _emptyStateMessage;
    private TextBlock? _previewCaption;
    private Bitmap? _bitmap;
    private bool _isAttached;

    public AnimationExchangeVisualPreviewControl()
    {
        InitializeComponent();
        _runtimePreviewHost = this.FindControl<ContentPresenter>("RuntimePreviewHost");
        _runtimeViewbox = this.FindControl<Viewbox>("RuntimeViewbox");
        _bitmapPreviewImage = this.FindControl<Image>("BitmapPreviewImage");
        _emptyStateBorder = this.FindControl<Border>("EmptyStateBorder");
        _emptyStateTitle = this.FindControl<TextBlock>("EmptyStateTitle");
        _emptyStateMessage = this.FindControl<TextBlock>("EmptyStateMessage");
        _previewCaption = this.FindControl<TextBlock>("PreviewCaption");
    }

    public AnimationExchangeFormat Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public string PreviewText
    {
        get => GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    public TimelineDocument? PreviewDocument
    {
        get => GetValue(PreviewDocumentProperty);
        set => SetValue(PreviewDocumentProperty, value);
    }

    public double PreviewTime
    {
        get => GetValue(PreviewTimeProperty);
        set => SetValue(PreviewTimeProperty, value);
    }

    public string PreviewSummary
    {
        get => GetValue(PreviewSummaryProperty);
        set => SetValue(PreviewSummaryProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        RefreshPreview();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        ClearPreviewState();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FormatProperty
            || change.Property == PreviewTextProperty
            || change.Property == PreviewDocumentProperty
            || change.Property == PreviewTimeProperty
            || change.Property == PreviewSummaryProperty)
        {
            RefreshPreview();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RefreshPreview()
    {
        if (!_isAttached)
        {
            return;
        }

        ClearPreviewState();
        if (Format == AnimationExchangeFormat.AvaloniaXaml && TryShowRuntimeXamlPreview())
        {
            return;
        }

        if (TryShowBitmapPreview())
        {
            return;
        }

        ShowEmptyState(
            "Preview unavailable",
            string.IsNullOrWhiteSpace(PreviewSummary)
                ? "Generate a preview to inspect the selected format."
                : PreviewSummary);
    }

    private bool TryShowRuntimeXamlPreview()
    {
        if (string.IsNullOrWhiteSpace(PreviewText))
        {
            return false;
        }

        try
        {
            var previewUri = new Uri("avares://TimelineAnimations.App/InteropPreview.axaml");
            var document = new RuntimeXamlLoaderDocument(previewUri, PreviewText)
            {
                Document = "InteropPreview.axaml"
            };
            var loaded = AvaloniaRuntimeXamlLoader.Load(
                document,
                new RuntimeXamlLoaderConfiguration
                {
                    LocalAssembly = typeof(AnimationExchangeVisualPreviewControl).Assembly,
                    UseCompiledBindingsByDefault = true,
                    CreateSourceInfo = true
                });

            if (loaded is Control control)
            {
                if (_runtimePreviewHost is not null)
                {
                    _runtimePreviewHost.Content = control;
                }

                if (_runtimeViewbox is not null)
                {
                    _runtimeViewbox.IsVisible = true;
                }

                if (_previewCaption is not null)
                {
                    _previewCaption.Text = string.IsNullOrWhiteSpace(PreviewSummary)
                        ? "Runtime Avalonia XAML preview loaded."
                        : PreviewSummary;
                }

                if (_emptyStateBorder is not null)
                {
                    _emptyStateBorder.IsVisible = false;
                }

                return true;
            }

            ShowEmptyState("Unsupported preview root", "The Avalonia runtime loader did not return a Control.");
            return true;
        }
        catch (Exception ex)
        {
            if (PreviewDocument is not null && TryShowBitmapPreview())
            {
                if (_previewCaption is not null)
                {
                    _previewCaption.Text =
                        $"Runtime XAML preview failed and fell back to the imported scene snapshot. {ex.Message}";
                }

                return true;
            }

            ShowEmptyState("Runtime XAML preview failed", ex.Message);
            return true;
        }
    }

    private bool TryShowBitmapPreview()
    {
        if (PreviewDocument is null || PreviewDocument.Scenes.Count == 0)
        {
            return false;
        }

        var activeScene = PreviewDocument.Scenes.FirstOrDefault(scene => scene.Id == PreviewDocument.ActiveSceneId)
            ?? PreviewDocument.Scenes[0];
        var width = (int)Math.Ceiling(Math.Max(1d, activeScene.CanvasWidth));
        var height = (int)Math.Ceiling(Math.Max(1d, activeScene.CanvasHeight));
        var time = TimelineMath.Clamp(PreviewTime, 0d, Math.Max(activeScene.Duration, 0.01d));

        _bitmap = FrameExportService.RenderFrameBitmap(
            PreviewDocument,
            time,
            width,
            height,
            activeScene.FrameRate,
            playAllScenes: false,
            transparentBackground: false);

        if (_bitmapPreviewImage is not null)
        {
            _bitmapPreviewImage.Source = _bitmap;
            _bitmapPreviewImage.IsVisible = true;
        }

        if (_previewCaption is not null)
        {
            _previewCaption.Text = string.IsNullOrWhiteSpace(PreviewSummary)
                ? "Visual preview rendered from the imported animation snapshot."
                : PreviewSummary;
        }

        if (_emptyStateBorder is not null)
        {
            _emptyStateBorder.IsVisible = false;
        }

        return true;
    }

    private void ShowEmptyState(string title, string message)
    {
        if (_runtimeViewbox is not null)
        {
            _runtimeViewbox.IsVisible = false;
        }

        if (_bitmapPreviewImage is not null)
        {
            _bitmapPreviewImage.IsVisible = false;
        }

        if (_emptyStateBorder is not null)
        {
            _emptyStateBorder.IsVisible = true;
        }

        if (_emptyStateTitle is not null)
        {
            _emptyStateTitle.Text = title;
        }

        if (_emptyStateMessage is not null)
        {
            _emptyStateMessage.Text = message;
        }

        if (_previewCaption is not null)
        {
            _previewCaption.Text = message;
        }
    }

    private void ClearPreviewState()
    {
        if (_runtimePreviewHost is not null)
        {
            _runtimePreviewHost.Content = null;
        }

        if (_runtimeViewbox is not null)
        {
            _runtimeViewbox.IsVisible = false;
        }

        if (_bitmapPreviewImage is not null)
        {
            _bitmapPreviewImage.Source = null;
            _bitmapPreviewImage.IsVisible = false;
        }

        _bitmap?.Dispose();
        _bitmap = null;
    }
}
