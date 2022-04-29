namespace Aeon.Emulator.Launcher;

using System.Collections.Generic;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;


/// <summary>
/// A dialog which presents choices to the user.
/// </summary>
public partial class TaskDialog : Window
{
    /// <summary>
    /// The Caption dependency property definition.
    /// </summary>
    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<TaskDialog, string>(nameof(Caption));
    /// <summary>
    /// The Items dependency property definition.
    /// </summary>
    public static readonly StyledProperty<IEnumerable<TaskDialogItem>> ItemsProperty = AvaloniaProperty.Register<TaskDialog, IEnumerable<TaskDialogItem>>(nameof(Items));

    /// <summary>
    /// Gets or sets the caption text to display in the dialog. This is a dependency property.
    /// </summary>
    public string Caption
    {
        get => (string)this.GetValue(CaptionProperty);
        set => this.SetValue(CaptionProperty, value);
    }
    /// <summary>
    /// Gets or sets the choices to display in the dialog. This is a dependency property.
    /// </summary>
    public IEnumerable<TaskDialogItem> Items
    {
        get => this.GetValue(ItemsProperty);
        set => this.SetValue(ItemsProperty, value);
    }
    /// <summary>
    /// Gets the item that has been selected in the dialog.
    /// </summary>
    public TaskDialogItem? SelectedItem { get; private set; }

    private void Item_Click(object? source, RoutedEventArgs e)
    {
        this.SelectedItem = e.Source as TaskDialogItem;
        this.Close(true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDialog"/> class.
    /// </summary>
    public TaskDialog()
    {
        InitializeComponent();
        this.AddHandler(Button.ClickEvent, Item_Click);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}