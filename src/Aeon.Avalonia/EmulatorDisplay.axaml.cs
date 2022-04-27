namespace Aeon.Emulator.Launcher;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;

public partial class EmulatorDisplay : Window
{
    private EmulatorHost? emulator;
    /// <summary>
    /// Gets or sets the emulator to display.
    /// </summary>
    public EmulatorHost EmulatorHost => emulator;
    public EmulatorDisplay()
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
