using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Aeon.Emulator.Launcher
{
    /// <summary>
    /// A simple integer numeric up/down control.
    /// </summary>
    [ContentProperty(nameof(Value))]
    public partial class NumericUpDown : UserControl
    {
        /// <summary>
        /// Defines the Value dependency property.
        /// </summary>
        public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<NumericUpDown, int>(nameof(Value), 0);
        /// <summary>
        /// Defines the MinimumValue dependency property.
        /// </summary>
        public static readonly StyledProperty<int> MinimumValueProperty = AvaloniaProperty.Register<NumericUpDown, int>(nameof(MinimumValue), 0);
        /// <summary>
        /// Defines the MaximumValue dependency property.
        /// </summary>
        public static readonly StyledProperty<int> MaximumValueProperty = AvaloniaProperty.Register<NumericUpDown, int>(nameof(MaximumValue), 100);
        /// <summary>
        /// Defines the StepValue dependency property.
        /// </summary>
        public static readonly StyledProperty<int> StepValueProperty = AvaloniaProperty.Register<NumericUpDown, int>(nameof(StepValue), 1);
        /// <summary>
        /// Defines the IsReadOnly dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<NumericUpDown, bool>(nameof(IsReadOnly), false);

        /// <summary>
        /// Initializes a new instance of the NumericUpDown class.
        /// </summary>
        public NumericUpDown()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Gets or sets the current value. This is a dependency property.
        /// </summary>
        public int Value
        {
            get => (int)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }
        /// <summary>
        /// Gets or sets the minimum value. This is a dependency property.
        /// </summary>
        public int MinimumValue
        {
            get => (int)this.GetValue(MinimumValueProperty);
            set => this.SetValue(MinimumValueProperty, value);
        }
        /// <summary>
        /// Gets or sets the maximum value. This is a dependency property.
        /// </summary>
        public int MaximumValue
        {
            get => (int)this.GetValue(MaximumValueProperty);
            set => this.SetValue(MaximumValueProperty, value);
        }
        /// <summary>
        /// Gets or sets the increment/decrement value. This is a dependency property.
        /// </summary>
        public int StepValue
        {
            get => (int)this.GetValue(StepValueProperty);
            set => this.SetValue(StepValueProperty, value);
        }
        /// <summary>
        /// Gets or sets a value indicating whether the text box part of the control is read-only. This is a dependency property.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)this.GetValue(IsReadOnlyProperty);
            set => this.SetValue(IsReadOnlyProperty, value);
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(ValueProperty, Math.Min(this.Value + this.StepValue, this.MaximumValue));
        }
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(ValueProperty, Math.Max(this.Value - this.StepValue, this.MinimumValue));
        }
        private static void MinimumValue_PropertyChanged(DependencyObject d, StyledPropertyChangedEventArgs e)
        {
            NumericUpDown control = (NumericUpDown)d;

            int newValue = (int)e.NewValue;
            if (newValue > control.Value)
                control.SetCurrentValue(ValueProperty, newValue);
        }
        private static void MaximumValue_PropertyChanged(DependencyObject d, StyledPropertyChangedEventArgs e)
        {
            NumericUpDown control = (NumericUpDown)d;

            int newValue = (int)e.NewValue;
            if (newValue < control.Value)
                control.SetCurrentValue(ValueProperty, newValue);
        }
        private static void Value_PropertyChanged(DependencyObject d, StyledPropertyChangedEventArgs e)
        {
            NumericUpDown control = (NumericUpDown)d;

            string text = control.valueText.Text;
            if (!string.IsNullOrEmpty(text))
            {
                if (int.TryParse(text, out int value) && value == (int)e.NewValue)
                    return;
            }

            control.valueText.Text = e.NewValue.ToString();
        }
        private static object Value_CoerceValue(DependencyObject d, object baseValue)
        {
            NumericUpDown control = (NumericUpDown)d;

            int value = (int)baseValue;
            if (value < control.MinimumValue)
                value = control.MinimumValue;
            if (value > control.MaximumValue)
                value = control.MaximumValue;

            return value;
        }
        private void ValueText_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9))
                e.Handled = true;
        }
        private void ValueText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = this.valueText.Text;
            if (!string.IsNullOrEmpty(text))
            {
                if (int.TryParse(text, out int value))
                {
                    if (this.Value != value)
                        SetCurrentValue(ValueProperty, value);
                }
            }
        }
    }
}
