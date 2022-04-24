using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aeon.Emulator.Launcher.Debugger
{
    /// <summary>
    /// Contains attached dependency properties for Aeon debugging.
    /// </summary>
    public class AeonDebug : Control
    {
        public static StyledProperty<IDebuggerTextSettings>? DebuggerTextSettingsProperty;

        public static StyledProperty<bool>? IsHexFormatProperty;

        /// <summary>
        /// Default debugger text format.
        /// </summary>
        private sealed class DefaultTextFormat : IDebuggerTextSettings
        {
            /// <summary>
            /// Initializes a new instance of the DefaultTextFormat class.
            /// </summary>
            public DefaultTextFormat()
            {
            }

            /// <summary>
            /// Gets the brush used for registers.
            /// </summary>
            public Brush Register => (Brush)Brushes.Blue;
            /// <summary>
            /// Gets the brush used for immediates.
            /// </summary>
            public Brush Immediate => (Brush)Brushes.Magenta;
            /// <summary>
            /// Gets the brush used for addresses.
            /// </summary>
            public Brush Address => (Brush)Brushes.Maroon;
        }
    }
}
