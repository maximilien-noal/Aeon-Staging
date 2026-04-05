using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// A simple integer numeric up/down control.
/// </summary>
public partial class NumericUpDown : UserControl
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<NumericUpDown, int>(nameof(Value), 0, coerce: CoerceValue);
    public static readonly StyledProperty<int> MinimumValueProperty =
        AvaloniaProperty.Register<NumericUpDown, int>(nameof(MinimumValue), 0);
    public static readonly StyledProperty<int> MaximumValueProperty =
        AvaloniaProperty.Register<NumericUpDown, int>(nameof(MaximumValue), 100);
    public static readonly StyledProperty<int> StepValueProperty =
        AvaloniaProperty.Register<NumericUpDown, int>(nameof(StepValue), 1);
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<NumericUpDown, bool>(nameof(IsReadOnly), false);

    static NumericUpDown()
    {
        ValueProperty.Changed.AddClassHandler<NumericUpDown>(OnValueChanged);
        MinimumValueProperty.Changed.AddClassHandler<NumericUpDown>(OnMinimumValueChanged);
        MaximumValueProperty.Changed.AddClassHandler<NumericUpDown>(OnMaximumValueChanged);
    }

    public NumericUpDown()
    {
        InitializeComponent();
    }

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MinimumValue
    {
        get => GetValue(MinimumValueProperty);
        set => SetValue(MinimumValueProperty, value);
    }

    public int MaximumValue
    {
        get => GetValue(MaximumValueProperty);
        set => SetValue(MaximumValueProperty, value);
    }

    public int StepValue
    {
        get => GetValue(StepValueProperty);
        set => SetValue(StepValueProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private void UpButton_Click(object? sender, RoutedEventArgs e)
    {
        Value = Math.Min(this.Value + this.StepValue, this.MaximumValue);
    }

    private void DownButton_Click(object? sender, RoutedEventArgs e)
    {
        Value = Math.Max(this.Value - this.StepValue, this.MinimumValue);
    }

    private static int CoerceValue(AvaloniaObject obj, int value)
    {
        var control = (NumericUpDown)obj;
        if (value < control.MinimumValue)
            value = control.MinimumValue;
        if (value > control.MaximumValue)
            value = control.MaximumValue;
        return value;
    }

    private static void OnValueChanged(NumericUpDown control, AvaloniaPropertyChangedEventArgs e)
    {
        var newValue = (int)e.NewValue!;
        string text = control.valueText.Text ?? string.Empty;
        if (!string.IsNullOrEmpty(text) && int.TryParse(text, out int parsed) && parsed == newValue)
            return;

        control.valueText.Text = newValue.ToString();
    }

    private static void OnMinimumValueChanged(NumericUpDown control, AvaloniaPropertyChangedEventArgs e)
    {
        int newMin = (int)e.NewValue!;
        if (newMin > control.Value)
            control.Value = newMin;
    }

    private static void OnMaximumValueChanged(NumericUpDown control, AvaloniaPropertyChangedEventArgs e)
    {
        int newMax = (int)e.NewValue!;
        if (newMax < control.Value)
            control.Value = newMax;
    }

    private void ValueText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            && e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Left && e.Key != Key.Right
            && e.Key != Key.Home && e.Key != Key.End && e.Key != Key.Tab
            && !(e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            && !(e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            && !(e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            e.Handled = true;
        }
    }

    private void ValueText_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = this.valueText.Text ?? string.Empty;
        if (!string.IsNullOrEmpty(text))
        {
            if (int.TryParse(text, out int value))
            {
                if (this.Value != value)
                    this.Value = value;
            }
        }
    }
}
