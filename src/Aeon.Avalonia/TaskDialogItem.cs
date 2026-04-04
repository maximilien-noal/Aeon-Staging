using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Represents an item in a task dialog.
/// </summary>
public class TaskDialogItem : Button
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialogItem"/> class.
    /// </summary>
    public TaskDialogItem()
    {
        this.HorizontalAlignment = HorizontalAlignment.Stretch;
        this.HorizontalContentAlignment = HorizontalAlignment.Left;
        this.Margin = new Avalonia.Thickness(0, 2);
        UpdateContent();
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialogItem"/> class.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="description">The description to display.</param>
    public TaskDialogItem(string text, string description)
        : this()
    {
        this.Text = text;
        this.Description = description;
        UpdateContent();
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string? Text { get; set; }
    /// <summary>
    /// Gets or sets the description to display.
    /// </summary>
    public string? Description { get; set; }

    private void UpdateContent()
    {
        var panel = new StackPanel { Spacing = 2 };
        if (!string.IsNullOrEmpty(this.Text))
            panel.Children.Add(new TextBlock { Text = this.Text, FontWeight = FontWeight.Bold });
        if (!string.IsNullOrEmpty(this.Description))
            panel.Children.Add(new TextBlock { Text = this.Description, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray });
        this.Content = panel;
    }
}
