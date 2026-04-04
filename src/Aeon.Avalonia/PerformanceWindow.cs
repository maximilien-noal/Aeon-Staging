using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Aeon.Emulator.Launcher;

public sealed class PerformanceWindow : Window
{
    private long lastCount;
    private DispatcherTimer? timer;
    private readonly TextBlock instructionsLabel;
    private readonly TextBlock ipsLabel;
    private readonly TextBlock conventionalMemoryLabel;
    private readonly TextBlock expandedMemoryLabel;
    private readonly TextBlock extendedMemoryLabel;
    private readonly Expander processorExpander;
    private readonly Expander memoryExpander;

    public PerformanceWindow()
    {
        this.Title = "Aeon Performance";
        this.Height = 300;
        this.Width = 500;
        this.ShowInTaskbar = false;
        this.MinWidth = 400;
        this.MinHeight = 200;

        this.instructionsLabel = new TextBlock { Text = "0", HorizontalAlignment = HorizontalAlignment.Right };
        this.ipsLabel = new TextBlock { Text = "0", HorizontalAlignment = HorizontalAlignment.Right };
        this.conventionalMemoryLabel = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
        this.expandedMemoryLabel = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
        this.extendedMemoryLabel = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };

        var processorGrid = new Grid
        {
            Margin = new Thickness(30, 0, 0, 0),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }
        };

        var instrLabel = new TextBlock { Text = "Instructions emulated:" };
        var ipsTextLabel = new TextBlock { Text = "Instructions per second:" };
        Grid.SetRow(instrLabel, 0); Grid.SetColumn(instrLabel, 0);
        Grid.SetRow(ipsTextLabel, 1); Grid.SetColumn(ipsTextLabel, 0);
        Grid.SetRow(this.instructionsLabel, 0); Grid.SetColumn(this.instructionsLabel, 1);
        Grid.SetRow(this.ipsLabel, 1); Grid.SetColumn(this.ipsLabel, 1);
        processorGrid.Children.Add(instrLabel);
        processorGrid.Children.Add(ipsTextLabel);
        processorGrid.Children.Add(this.instructionsLabel);
        processorGrid.Children.Add(this.ipsLabel);

        this.processorExpander = new Expander
        {
            Header = new TextBlock { Text = "Processor", FontSize = 16, Foreground = Brushes.SteelBlue },
            IsExpanded = true,
            Content = processorGrid
        };

        var memoryGrid = new Grid
        {
            Margin = new Thickness(30, 0, 0, 0),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }
        };

        var convLabel = new TextBlock { Text = "Conventional memory:" };
        var expLabel = new TextBlock { Text = "Expanded memory:" };
        var extLabel = new TextBlock { Text = "Extended memory:" };
        Grid.SetRow(convLabel, 0); Grid.SetColumn(convLabel, 0);
        Grid.SetRow(expLabel, 1); Grid.SetColumn(expLabel, 0);
        Grid.SetRow(extLabel, 2); Grid.SetColumn(extLabel, 0);
        Grid.SetRow(this.conventionalMemoryLabel, 0); Grid.SetColumn(this.conventionalMemoryLabel, 1);
        Grid.SetRow(this.expandedMemoryLabel, 1); Grid.SetColumn(this.expandedMemoryLabel, 1);
        Grid.SetRow(this.extendedMemoryLabel, 2); Grid.SetColumn(this.extendedMemoryLabel, 1);
        memoryGrid.Children.Add(convLabel);
        memoryGrid.Children.Add(expLabel);
        memoryGrid.Children.Add(extLabel);
        memoryGrid.Children.Add(this.conventionalMemoryLabel);
        memoryGrid.Children.Add(this.expandedMemoryLabel);
        memoryGrid.Children.Add(this.extendedMemoryLabel);

        this.memoryExpander = new Expander
        {
            Header = new TextBlock { Text = "Memory", FontSize = 16, Foreground = Brushes.SteelBlue },
            IsExpanded = true,
            Content = memoryGrid
        };

        var titleLabel = new TextBlock
        {
            Text = "Emulator Statistics",
            FontSize = 20,
            Foreground = Brushes.DarkBlue,
            Margin = new Thickness(5)
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(20, 0, 0, 0),
            Children =
            {
                this.processorExpander,
                this.memoryExpander
            }
        };

        var scrollViewer = new ScrollViewer { Content = stack };

        var dockPanel = new DockPanel();
        DockPanel.SetDock(titleLabel, Dock.Top);
        dockPanel.Children.Add(titleLabel);
        dockPanel.Children.Add(scrollViewer);

        this.Content = dockPanel;
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
