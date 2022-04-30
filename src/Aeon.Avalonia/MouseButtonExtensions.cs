
using Avalonia.Input;

namespace Aeon.Emulator.Launcher
{
    /// <summary>
    /// Contains extension methods for the Avalonia.Input.MouseButton type.
    /// </summary>
    public static class MouseButtonExtensions
    {
        /// <summary>
        /// Returns an Aeon.Emulator.MouseButtons value from a Avalonia.Input.PointerPointProperties value.
        /// </summary>
        /// <param name="mouseButton">Avalonia.Input.MouseButton value to convert.</param>
        /// <returns>Aeon.Emulator.MouseButtons value.</returns>
        public static MouseButtons ToEmulatorButtons(this PointerPointProperties mouseButton)
        {
            if(mouseButton.IsLeftButtonPressed) 
                return MouseButtons.Left;
            if(mouseButton.IsMiddleButtonPressed)
                return MouseButtons.Middle;
            if(mouseButton.IsRightButtonPressed)
                return MouseButtons.Right;
            return MouseButtons.None;
        }

        /// <summary>
        /// Returns an Aeon.Emulator.MouseButtons value from a Avalonia.Input.MouseButton value.
        /// </summary>
        /// <param name="mouseButton">Avalonia.Input.MouseButton value to convert.</param>
        /// <returns>Aeon.Emulator.MouseButtons value.</returns>
        public static MouseButtons ToEmulatorButtons(this MouseButton mouseButton)
        {
            return mouseButton switch
            {
                MouseButton.Left => MouseButtons.Left,
                MouseButton.Middle => MouseButtons.Middle,
                MouseButton.Right => MouseButtons.Right,
                _ => MouseButtons.None,
            };
        }
    }
}
