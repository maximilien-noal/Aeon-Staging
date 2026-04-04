using Avalonia.Controls;
using Avalonia.Threading;

namespace Aeon.Emulator.Launcher;

public sealed partial class PerformanceWindow : Window
{
    private long lastCount;
    private DispatcherTimer? timer;

    public PerformanceWindow()
    {
        InitializeComponent();
    }

    public EmulatorDisplay? EmulatorDisplay { get; set; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, Timer_Tick);
        timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        timer?.Stop();
        base.OnClosed(e);
    }

    private void UpdateProcessorFields(EmulatorHost host)
    {
        long currentCount = host.TotalInstructions;
        if (currentCount < lastCount)
            lastCount = 0;

        long value = currentCount - lastCount;

        instructionsLabel.Text = currentCount.ToString("#,#");
        ipsLabel.Text = value.ToString("#,#");

        lastCount = currentCount;
    }

    private void UpdateMemoryFields(EmulatorHost host)
    {
        var conventionalMemory = host.VirtualMachine.GetConventionalMemoryUsage();
        conventionalMemoryLabel.Text = $"{conventionalMemory.MemoryUsed / 1024}k used, {conventionalMemory.MemoryFree / 1024}k free";

        var expandedMemory = host.VirtualMachine.GetExpandedMemoryUsage();
        expandedMemoryLabel.Text = $"{expandedMemory.BytesAllocated / 1024}k used, {expandedMemory.BytesFree / 1024}k free";

        var extendedMemory = host.VirtualMachine.GetExtendedMemoryUsage();
        extendedMemoryLabel.Text = $"{extendedMemory.BytesAllocated / 1024}k used, {extendedMemory.BytesFree / 1024}k free";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var display = this.EmulatorDisplay;
        if (display != null)
        {
            var host = display.EmulatorHost;
            if (host != null)
            {
                if (processorExpander.IsExpanded)
                    this.UpdateProcessorFields(host);

                if (memoryExpander.IsExpanded)
                    this.UpdateMemoryFields(host);
            }
        }
    }
}
