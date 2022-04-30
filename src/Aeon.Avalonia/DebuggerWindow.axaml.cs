using CommunityToolkit.Mvvm.ComponentModel;

namespace Aeon.Emulator.Launcher;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;

public partial class DebuggerWindow : Window
{
    public DebuggerWindow()
    {
        Initialize();
    }

    private static readonly StyledProperty<EmulatorHost?> EmulatorHostProperty = AvaloniaProperty.Register<DebuggerWindow, EmulatorHost?>(nameof(EmulatorHost));

    public EmulatorHost? EmulatorHost
    {
        get => this.GetValue(EmulatorHostProperty);
        set => this.SetValue(EmulatorHostProperty, value);
    }

    public DebuggerWindow(WindowBase owner)
    {
        this.Owner = owner;
        Initialize();
    }

    private void Initialize()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void UpdateDebugger()
    {
    }
}
