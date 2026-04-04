using Avalonia.Interactivity;

namespace Aeon.Emulator.Launcher;

public sealed class EmulationErrorRoutedEventArgs : RoutedEventArgs
{
    public EmulationErrorRoutedEventArgs(RoutedEvent routedEvent, string message)
        : base(routedEvent)
    {
        this.Message = message;
    }

    public string Message { get; }
}

public delegate void EmulationErrorRoutedEventHandler(object? sender, EmulationErrorRoutedEventArgs e);
