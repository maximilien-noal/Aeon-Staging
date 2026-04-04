using Avalonia.Input;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Contains extension methods for Avalonia pointer input.
/// </summary>
public static class MouseButtonExtensions
{
    /// <summary>
    /// Returns an Aeon.Emulator.MouseButtons value from an Avalonia PointerUpdateKind.
    /// </summary>
    /// <param name="kind">PointerUpdateKind to convert.</param>
    /// <returns>Aeon.Emulator.MouseButtons value.</returns>
    public static MouseButtons ToEmulatorButtons(this PointerUpdateKind kind)
    {
        return kind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MouseButtons.Left,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MouseButtons.Middle,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MouseButtons.Right,
            _ => MouseButtons.None,
        };
    }
}
