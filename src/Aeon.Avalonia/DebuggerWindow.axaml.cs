namespace Aeon.Emulator.Launcher;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;

public partial class DebuggerWindow : Window
{
    public DebuggerWindow()
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
