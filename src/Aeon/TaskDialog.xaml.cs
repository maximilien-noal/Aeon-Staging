using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Aeon.Emulator.Launcher
{
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
        public static readonly StyledProperty<IEnumerable<TaskDialogItem>?> ItemsProperty = AvaloniaProperty.Register<TaskDialog, IEnumerable<TaskDialogItem>?>(nameof(Items));

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialog"/> class.
        /// </summary>
        public TaskDialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.AddHandler(Button.ClickEvent, new EventHandler<RoutedEventArgs>(this.Item_Click));
        }

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
            get => (IEnumerable<TaskDialogItem>)this.GetValue(ItemsProperty);
            set => this.SetValue(ItemsProperty, value);
        }
        /// <summary>
        /// Gets the item that has been selected in the dialog.
        /// </summary>
        public TaskDialogItem SelectedItem { get; private set; }

        private void Item_Click(object source, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.SelectedItem = e.Source as TaskDialogItem;
            this.Close();
        }
    }
}
