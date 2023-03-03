﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;

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
        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(nameof(Caption), typeof(string), typeof(TaskDialog));
        /// <summary>
        /// The Items dependency property definition.
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(IEnumerable<TaskDialogItem>), typeof(TaskDialog));

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialog"/> class.
        /// </summary>
        public TaskDialog()
        {
            this.InitializeComponent();
            this.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(this.Item_Click));
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
            this.SelectedItem = e.OriginalSource as TaskDialogItem;
            this.Close();
        }
    }
}
