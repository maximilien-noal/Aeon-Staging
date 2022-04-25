namespace Aeon.Emulator.Launcher;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;

public partial class PaletteDialog : Window
{
    public PaletteDialog()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
