using Avalonia.Input;

namespace Aeon.Emulator.Launcher;

public static class MouseButtonExtensions
{
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
