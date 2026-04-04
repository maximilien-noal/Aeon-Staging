using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Aeon.Emulator.Launcher;

public partial class PaletteDialog : Window
{
    private DispatcherTimer? timer;

    public PaletteDialog()
    {
        InitializeComponent();
    }

    public EmulatorDisplay? EmulatorDisplay { get; set; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        for (int i = 0; i < 256; i++)
            this.grid.Children.Add(new Rectangle { Fill = new SolidColorBrush() });

        this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / 30.0), DispatcherPriority.Normal, UpdateColors);
        this.timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        this.timer?.Stop();
        base.OnClosed(e);
    }

    private uint[]? GetPalette()
    {
        var display = this.EmulatorDisplay;
        if (display?.EmulatorHost?.VirtualMachine?.VideoMode != null)
            return display.EmulatorHost.VirtualMachine.VideoMode.Palette.ToArray();
        return null;
    }

    private void UpdateColors(object? sender, EventArgs e)
    {
        var palette = this.GetPalette();
        if (palette == null)
            return;

        for (int i = 0; i < palette.Length && i < this.grid.Children.Count; i++)
        {
            if (this.grid.Children[i] is Rectangle rect && rect.Fill is SolidColorBrush brush)
                brush.Color = Color.FromRgb((byte)(palette[i] >> 16), (byte)(palette[i] >> 8), (byte)palette[i]);
        }
    }
}
