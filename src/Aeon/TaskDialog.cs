using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aeon.Emulator.Launcher;

public partial class TaskDialog : Window
{
    public TaskDialog()
    {
        InitializeComponent();
    }

    public string? Caption
    {
        get => this.captionText.Text;
        set => this.captionText.Text = value;
    }

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

    public TaskDialogItem? SelectedItem { get; private set; }

    private void Item_Click(object? sender, RoutedEventArgs e)
    {
        this.SelectedItem = sender as TaskDialogItem;
        this.Close(true);
    }
}
