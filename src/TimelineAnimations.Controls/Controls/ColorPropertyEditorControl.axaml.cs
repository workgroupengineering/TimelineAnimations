using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using TimelineAnimations.App.Helpers;

namespace TimelineAnimations.App.Controls;

public partial class ColorPropertyEditorControl : UserControl
{
    public static readonly DirectProperty<ColorPropertyEditorControl, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<ColorPropertyEditorControl, string>(
            nameof(Value),
            editor => editor.Value,
            (editor, value) => editor.Value = value,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private static readonly string[] PresetColors =
    [
        "#FFFFFF",
        "#09111F",
        "#182748",
        "#000000",
        "#46C9FF",
        "#61E6FF",
        "#24E5C1",
        "#57C9FF",
        "#F0C44B",
        "#F3A33C",
        "#E46E6E",
        "#7C4DFF"
    ];

    private string _value = string.Empty;
    private bool _isSynchronizing;
    private Color _currentColor = Colors.White;
    private Color _previousColor = Colors.White;
    private Grid? _editorRoot;
    private Button? _swatchButton;
    private Border? _swatchPreview;
    private TextBox? _valueTextBox;
    private Popup? _pickerPopup;
    private Border? _currentPreviewBorder;
    private Border? _previousPreviewBorder;
    private TextBox? _hexTextBox;
    private Slider? _redSlider;
    private Slider? _greenSlider;
    private Slider? _blueSlider;
    private TextBlock? _redValueText;
    private TextBlock? _greenValueText;
    private TextBlock? _blueValueText;
    private WrapPanel? _presetPanel;
    private Button? _revertButton;

    public ColorPropertyEditorControl()
    {
        InitializeComponent();
        InitializeParts();
        InitializePresetButtons();
        SynchronizeUiFromValue();
    }

    public string Value
    {
        get => _value;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetAndRaise(ValueProperty, ref _value, normalized))
            {
                SynchronizeUiFromValue();
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeParts()
    {
        _editorRoot = this.FindControl<Grid>("EditorRoot");
        _swatchButton = this.FindControl<Button>("SwatchButton");
        _swatchPreview = this.FindControl<Border>("SwatchPreview");
        _valueTextBox = this.FindControl<TextBox>("ValueTextBox");
        _pickerPopup = this.FindControl<Popup>("PickerPopup");
        _currentPreviewBorder = this.FindControl<Border>("CurrentPreviewBorder");
        _previousPreviewBorder = this.FindControl<Border>("PreviousPreviewBorder");
        _hexTextBox = this.FindControl<TextBox>("HexTextBox");
        _redSlider = this.FindControl<Slider>("RedSlider");
        _greenSlider = this.FindControl<Slider>("GreenSlider");
        _blueSlider = this.FindControl<Slider>("BlueSlider");
        _redValueText = this.FindControl<TextBlock>("RedValueText");
        _greenValueText = this.FindControl<TextBlock>("GreenValueText");
        _blueValueText = this.FindControl<TextBlock>("BlueValueText");
        _presetPanel = this.FindControl<WrapPanel>("PresetPanel");
        _revertButton = this.FindControl<Button>("RevertButton");

        if (_pickerPopup is not null)
        {
            _pickerPopup.PlacementTarget = _editorRoot;
        }

        if (_swatchButton is not null)
        {
            _swatchButton.Click += (_, _) => OpenPicker();
        }

        if (_valueTextBox is not null)
        {
            _valueTextBox.PointerPressed += HandleEditorPointerPressed;
            _valueTextBox.PropertyChanged += HandleValueTextBoxPropertyChanged;
        }

        if (_hexTextBox is not null)
        {
            _hexTextBox.PropertyChanged += HandleHexTextBoxPropertyChanged;
        }

        if (_redSlider is not null)
        {
            _redSlider.PropertyChanged += HandleColorSliderPropertyChanged;
        }

        if (_greenSlider is not null)
        {
            _greenSlider.PropertyChanged += HandleColorSliderPropertyChanged;
        }

        if (_blueSlider is not null)
        {
            _blueSlider.PropertyChanged += HandleColorSliderPropertyChanged;
        }

        if (_revertButton is not null)
        {
            _revertButton.Click += (_, _) => CommitColor(_previousColor, closePopup: true);
        }
    }

    private void InitializePresetButtons()
    {
        if (_presetPanel is null || _presetPanel.Children.Count > 0)
        {
            return;
        }

        foreach (var hex in PresetColors)
        {
            var swatch = new Border
            {
                Background = ColorHelpers.Brush(hex),
                CornerRadius = new CornerRadius(4)
            };
            var button = new Button
            {
                Width = 22,
                Height = 22,
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                Content = swatch
            };
            button.Classes.Add("color-preset-button");
            ToolTip.SetTip(button, hex);
            button.Click += (_, _) => CommitColor(ColorHelpers.Parse(hex), closePopup: true);
            _presetPanel.Children.Add(button);
        }
    }

    private void HandleEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            OpenPicker();
        }
    }

    private void HandleValueTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && sender is TextBox textBox)
        {
            HandleMainTextChanged(textBox.Text);
        }
    }

    private void HandleHexTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && sender is TextBox textBox)
        {
            HandlePopupHexChanged(textBox.Text);
        }
    }

    private void HandleColorSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeBase.ValueProperty)
        {
            HandleSliderChanged();
        }
    }

    private void HandleMainTextChanged(string? text)
    {
        if (_isSynchronizing)
        {
            return;
        }

        Value = text ?? string.Empty;
    }

    private void HandlePopupHexChanged(string? text)
    {
        if (_isSynchronizing)
        {
            return;
        }

        Value = text ?? string.Empty;
    }

    private void HandleSliderChanged()
    {
        if (_isSynchronizing || _redSlider is null || _greenSlider is null || _blueSlider is null)
        {
            return;
        }

        var color = Color.FromRgb(
            (byte)Math.Clamp(Math.Round(_redSlider.Value), 0d, 255d),
            (byte)Math.Clamp(Math.Round(_greenSlider.Value), 0d, 255d),
            (byte)Math.Clamp(Math.Round(_blueSlider.Value), 0d, 255d));

        CommitColor(color);
    }

    private void OpenPicker()
    {
        if (_pickerPopup is null)
        {
            return;
        }

        _previousColor = ResolveColor(Value, _currentColor);
        SynchronizeUiFromValue();
        _pickerPopup.IsOpen = true;
    }

    private void CommitColor(Color color, bool closePopup = false)
    {
        Value = FormatColor(color);
        if (closePopup && _pickerPopup is not null)
        {
            _pickerPopup.IsOpen = false;
        }
    }

    private void SynchronizeUiFromValue()
    {
        _isSynchronizing = true;
        try
        {
            var resolved = ResolveColor(Value, _currentColor);
            _currentColor = resolved;

            if (_valueTextBox is not null && !string.Equals(_valueTextBox.Text, Value, StringComparison.Ordinal))
            {
                _valueTextBox.Text = Value;
            }

            if (_hexTextBox is not null && !string.Equals(_hexTextBox.Text, Value, StringComparison.Ordinal))
            {
                _hexTextBox.Text = Value;
            }

            if (_redSlider is not null)
            {
                _redSlider.Value = resolved.R;
            }

            if (_greenSlider is not null)
            {
                _greenSlider.Value = resolved.G;
            }

            if (_blueSlider is not null)
            {
                _blueSlider.Value = resolved.B;
            }

            if (_redValueText is not null)
            {
                _redValueText.Text = resolved.R.ToString(CultureInfo.InvariantCulture);
            }

            if (_greenValueText is not null)
            {
                _greenValueText.Text = resolved.G.ToString(CultureInfo.InvariantCulture);
            }

            if (_blueValueText is not null)
            {
                _blueValueText.Text = resolved.B.ToString(CultureInfo.InvariantCulture);
            }

            if (_swatchPreview is not null)
            {
                _swatchPreview.Background = ColorHelpers.Brush(FormatColor(resolved));
            }

            if (_currentPreviewBorder is not null)
            {
                _currentPreviewBorder.Background = ColorHelpers.Brush(FormatColor(resolved));
            }

            if (_previousPreviewBorder is not null)
            {
                _previousPreviewBorder.Background = ColorHelpers.Brush(FormatColor(_previousColor));
            }
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private static Color ResolveColor(string? value, Color fallback)
    {
        return !string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static string FormatColor(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
