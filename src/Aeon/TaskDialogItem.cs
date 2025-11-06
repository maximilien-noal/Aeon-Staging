using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Media;

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
        public static readonly StyledProperty<typeof(ImageSource), typeof(TaskDialogItem));
        /// <summary> IconProperty = AvaloniaProperty.Register<nameof(Icon), typeof(ImageSource), typeof(TaskDialogItem));
        /// <summary>
        /// The Text depdendency property definition.
        /// </summary>
        public static readonly StyledProperty<typeof(string), typeof(TaskDialogItem));
        /// <summary> TextProperty = AvaloniaProperty.Register<nameof(Text), typeof(string), typeof(TaskDialogItem));
        /// <summary>
        /// The Description dependency property definition.
        /// </summary>
        public static readonly StyledProperty<typeof(string), typeof(TaskDialogItem));

        /// <summary> DescriptionProperty = AvaloniaProperty.Register<nameof(Description), typeof(string), typeof(TaskDialogItem));

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
        public ImageSource Icon
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
