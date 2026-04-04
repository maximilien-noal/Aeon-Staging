using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// A dialog which presents choices to the user.
/// </summary>
public class TaskDialog : Window
{
    private readonly StackPanel itemsPanel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialog"/> class.
    /// </summary>
    public TaskDialog()
    {
        this.Width = 320;
        this.SizeToContent = SizeToContent.Height;
        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        this.ShowInTaskbar = false;
        this.CanResize = false;

        var captionBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = Brushes.DarkBlue,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap
        };

        this.itemsPanel = new StackPanel();

        var grid = new Grid
        {
            Margin = new Thickness(5),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };

        Grid.SetRow(captionBlock, 0);
        Grid.SetRow(this.itemsPanel, 1);
        grid.Children.Add(captionBlock);
        grid.Children.Add(this.itemsPanel);

        this.Content = grid;
        this.captionBlock = captionBlock;
    }

    private readonly TextBlock captionBlock;

    /// <summary>
    /// Gets or sets the caption text to display in the dialog.
    /// </summary>
    public string? Caption
    {
        get => this.captionBlock.Text;
        set => this.captionBlock.Text = value;
    }
    /// <summary>
    /// Gets or sets the choices to display in the dialog.
    /// </summary>
    public IEnumerable<TaskDialogItem>? Items
    {
        get;
        set
        {
            field = value;
            this.itemsPanel.Children.Clear();
            if (value != null)
            {
                foreach (var item in value)
                {
                    item.Click += Item_Click;
                    this.itemsPanel.Children.Add(item);
                }
            }
        }
    }
    /// <summary>
    /// Gets the item that has been selected in the dialog.
    /// </summary>
    public TaskDialogItem? SelectedItem { get; private set; }

    private void Item_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.SelectedItem = sender as TaskDialogItem;
        this.Close(true);
    }
}
