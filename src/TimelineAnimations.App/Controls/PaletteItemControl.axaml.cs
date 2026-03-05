using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TimelineAnimations.App.ViewModels;

namespace TimelineAnimations.App.Controls;

public partial class PaletteItemControl : UserControl
{
    public PaletteItemControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not PaletteItemViewModel item)
        {
            return;
        }

        var payload = new DataTransfer();
        payload.Add(DataTransferItem.CreateText($"palette:{item.Kind}"));
        await DragDrop.DoDragDropAsync(e, payload, DragDropEffects.Copy);
    }
}
