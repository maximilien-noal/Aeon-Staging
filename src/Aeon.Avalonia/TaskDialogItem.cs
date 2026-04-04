using Avalonia;
using Avalonia.Controls;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Represents an item in a task dialog.
/// </summary>
public class TaskDialogItem : Button
{
    /// <summary>
    /// The Text dependency property definition.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TaskDialogItem, string?>(nameof(Text));
    /// <summary>
    /// The Description dependency property definition.
    /// </summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<TaskDialogItem, string?>(nameof(Description));

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialogItem"/> class.
    /// </summary>
    public TaskDialogItem()
    {
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
    }

    /// <summary>
    /// Gets or sets the text to display. This is a styled property.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    /// <summary>
    /// Gets or sets the description to display. This is a styled property.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
