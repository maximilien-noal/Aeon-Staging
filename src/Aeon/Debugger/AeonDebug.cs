using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Aeon.Emulator.Launcher.Debugger
{
    /// <summary>
    /// Contains attached dependency properties for Aeon debugging.
    /// </summary>
    public static class AeonDebug
    {
        /// <summary>
        /// The AeonDebug.IsHexFormat dependency property definition.
        /// </summary>
        public static readonly AttachedProperty<bool> IsHexFormatProperty = AvaloniaProperty.RegisterAttached<bool>("IsHexFormat", typeof(AeonDebug), false, inherits: true);
        /// <summary>
        /// The AeonDebug.DebuggerTextFormat dependency property definition.
        /// </summary>
        public static readonly AttachedProperty<IDebuggerTextSettings> DebuggerTextFormatProperty = AvaloniaProperty.RegisterAttached<IDebuggerTextSettings>("DebuggerTextFormat", typeof(AeonDebug), new DefaultTextFormat(), inherits: true);

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
