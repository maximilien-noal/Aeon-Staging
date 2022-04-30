namespace Aeon.Emulator.Launcher;

using System;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Threading;

public partial class PerformanceWindow : Window
{
    private long lastCount;
    private DispatcherTimer timer;
    public EmulatorDisplay EmulatorDisplay { get; set; }

    protected override void OnInitialized()
    {
        timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, Timer_Tick);
        timer.Start();

        base.OnInitialized();
    }

    private void UpdateProcessorFields(EmulatorHost host)
    {
        long currentCount = host.TotalInstructions;
        if (currentCount < lastCount)
            lastCount = 0;

        long value = currentCount - lastCount;

        instructionsLabel.Content = currentCount.ToString("#,#");
        ipsLabel.Content = value.ToString("#,#");

        lastCount = currentCount;
    }
    private void UpdateMemoryFields(EmulatorHost host)
    {
        var conventionalMemory = host.VirtualMachine.GetConventionalMemoryUsage();
        conventionalMemoryLabel.Content = string.Format("{0}k used, {1}k free", conventionalMemory.MemoryUsed / 1024, conventionalMemory.MemoryFree / 1024);

        var expandedMemory = host.VirtualMachine.GetExpandedMemoryUsage();
        expandedMemoryLabel.Content = string.Format("{0}k used, {1}k free", expandedMemory.BytesAllocated / 1024, expandedMemory.BytesFree / 1024);

        var extendedMemory = host.VirtualMachine.GetExtendedMemoryUsage();
        extendedMemoryLabel.Content = string.Format("{0}k used, {1}k free", extendedMemory.BytesAllocated / 1024, extendedMemory.BytesFree / 1024);
    }

    private void Timer_Tick(object sender, EventArgs e)
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
    public PerformanceWindow()
    {
        Initialize();
    }

    private void Initialize()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public PerformanceWindow(WindowBase owner)
    {
        this.Owner = owner;
        Initialize();
    }
}
