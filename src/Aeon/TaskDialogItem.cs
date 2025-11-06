using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Aeon.Emulator.Launcher
{
    /// <summary>
    /// Represents an item in a task dialog.
    /// </summary>
    public class TaskDialogItem : Button
    {
        /// <summary>
        /// The Icon dependency property definition.
        /// </summary>
        public static readonly StyledProperty<Avalonia.Media.IImage?> IconProperty = AvaloniaProperty.Register<TaskDialogItem, ImageSource>(nameof(Icon));
        /// <summary>
        /// The Text depdendency property definition.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TaskDialogItem, string>(nameof(Text));
        /// <summary>
        /// The Description dependency property definition.
        /// </summary>
        public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<TaskDialogItem, string>(nameof(Description));

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialogItem"/> class.
        /// </summary>
        public TaskDialogItem()
        {
            this.BeginInit();
            this.EndInit();
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
        /// Gets or sets the icon to display. This is a dependency property.
        /// </summary>
        public Avalonia.Media.IImage? Icon
        {
            get => (ImageSource)this.GetValue(IconProperty);
            set => this.SetValue(IconProperty, value);
        }
        /// <summary>
        /// Gets or sets the text to display. This is a dependency property.
        /// </summary>
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }
        /// <summary>
        /// Gets or sets the description to display. This is a dependency property.
        /// </summary>
        public string Description
        {
            get => (string)this.GetValue(DescriptionProperty);
            set => this.SetValue(DescriptionProperty, value);
        }
    }
}
