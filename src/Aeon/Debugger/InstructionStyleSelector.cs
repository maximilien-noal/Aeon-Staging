using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Aeon.Emulator.DebugSupport;

namespace Aeon.Emulator.Launcher.Debugger
{
    /// <summary>
    /// Selects the appropriate style for an instruction.
    /// </summary>
    public sealed class InstructionStyleSelector : IStyleSelector
    {
        /// <summary>
        /// Selects a style for an item.
        /// </summary>
        /// <param name="item">The content.</param>
        /// <param name="container">The element to which the style will be applied.</param>
        /// <returns>
        /// Returns an application-specific style to apply; otherwise, null.
        /// </returns>
        public IStyle? Select(object? item, Control container)
        {
            if (item is LoggedInstruction)
                return container.FindResource("loggedInstructionStyle") as IStyle;
            else
                return container.FindResource("listBoxItemStyle") as IStyle;
        }
    }
}
