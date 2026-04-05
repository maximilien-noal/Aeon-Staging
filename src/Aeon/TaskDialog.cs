using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// A dialog which presents choices to the user.
/// </summary>
public partial class TaskDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialog"/> class.
    /// </summary>
    public TaskDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the caption text to display in the dialog.
    /// </summary>
    public string? Caption
    {
        get => this.captionText.Text;
        set => this.captionText.Text = value;
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
            this.itemsControl.ItemsSource = null;
            if (value != null)
            {
                foreach (var item in value)
                {
                    item.Click += Item_Click;
                }
                this.itemsControl.ItemsSource = value;
            }
        }
    }

    /// <summary>
    /// Gets the item that has been selected in the dialog.
    /// </summary>
    public TaskDialogItem? SelectedItem { get; private set; }

    private void Item_Click(object? sender, RoutedEventArgs e)
    {
        this.SelectedItem = sender as TaskDialogItem;
        this.Close(true);
    }
}
