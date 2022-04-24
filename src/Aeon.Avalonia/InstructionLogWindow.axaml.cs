namespace Aeon.Emulator.Launcher;

using System.Globalization;

using Aeon.Avalonia;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;

internal sealed partial class InstructionLogWindow : Window
{
    public InstructionLogWindow()
    {
        InitializeComponent();
        this.NextButton.Click += NextAddress_Click;
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static void ShowDialog(LogAccessor log)
    {
        var window = new InstructionLogWindow { Owner = App.MainWindow };
        window.historyList.Items = log;
        window.Show();
    }

    private bool TryReadAddress(out ushort segment, out uint offset)
    {
        segment = 0;
        offset = 0;

        var parts = this.gotoAddressBox.Text.Trim().Split(':');
        if (parts.Length != 2)
            return false;

        if (!ushort.TryParse(parts[0], NumberStyles.HexNumber, null, out segment))
            return false;

        if (!uint.TryParse(parts[1], NumberStyles.HexNumber, null, out offset))
            return false;

        return true;
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.historyList.SelectedItem is DebugLogItem item)
            this.registerText.Text = item.RegisterText;
    }
    private void FindNextError_Click(object sender, RoutedEventArgs e)
    {
        if (this.historyList.Items is LogAccessor log)
        {
            int index = log.FindNextError(this.historyList.SelectedIndex + 1);
            if (index >= 0)
            {
                this.historyList.SelectedIndex = index;
                this.historyList.ScrollIntoView(this.historyList.SelectedItem);
            }
        }
    }

    private void NextAddress_Click(object sender, RoutedEventArgs e)
    {
        if (!this.TryReadAddress(out ushort segment, out uint offset))
            return;

        var log = (LogAccessor)this.historyList.Items;
        int i = 0;
        int selectedIndex = this.historyList.SelectedIndex;

        foreach (var item in log)
        {
            if (i > selectedIndex && item.CS == segment && item.EIP == offset)
            {
                this.historyList.SelectedIndex = i;
                this.historyList.ScrollIntoView(item);
                return;
            }

            i++;
        }
    }

    private void NextV86_Click(object sender, RoutedEventArgs e)
    {
        var log = (LogAccessor)this.historyList.Items;
        int i = 0;
        int selectedIndex = this.historyList.SelectedIndex;

        bool current = false;

        if (this.historyList.SelectedItem is DebugLogItem currentItem)
            current = currentItem.Flags.HasFlag(EFlags.Virtual8086Mode);

        foreach (var item in log)
        {
            if (i > selectedIndex && item.Flags.HasFlag(EFlags.Virtual8086Mode) != current)
            {
                this.historyList.SelectedIndex = i;
                this.historyList.ScrollIntoView(item);
                return;
            }

            i++;
        }
    }

    private void LastV86_Click(object sender, RoutedEventArgs e)
    {
        var log = (LogAccessor)this.historyList.Items;
        int i = 0;
        int selectedIndex = this.historyList.SelectedIndex;

        bool current = false;

        if (this.historyList.SelectedItem is DebugLogItem currentItem)
            current = currentItem.Flags.HasFlag(EFlags.Virtual8086Mode);

        int foundIndex = 0;

        foreach (var item in log)
        {
            if (i >= selectedIndex)
                break;

            if (item.Flags.HasFlag(EFlags.Virtual8086Mode) != current)
                foundIndex = i;

            i++;
        }

        if (foundIndex >= 0)
        {
            this.historyList.SelectedIndex = foundIndex;
            this.historyList.ScrollIntoView(this.historyList.SelectedItem);
        }
    }
}
