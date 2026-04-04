using Avalonia;
using Avalonia.Controls;

namespace Aeon.Emulator.Launcher;

public class TaskDialogItem : Button
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TaskDialogItem, string?>(nameof(Text));
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<TaskDialogItem, string?>(nameof(Description));

    public TaskDialogItem()
    {
    }

    public TaskDialogItem(string text, string description)
        : this()
    {
        this.Text = text;
        this.Description = description;
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
