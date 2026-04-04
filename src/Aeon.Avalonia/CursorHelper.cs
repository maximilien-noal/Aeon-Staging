using System.Runtime.InteropServices;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Cross-platform cursor warping helper using OS system libraries only.
/// Ported from SDL2 cursor handling code (pure C#, no SDL2 dependency).
/// </summary>
internal static class CursorHelper
{
    /// <summary>
    /// Warps the mouse cursor to the specified screen coordinates.
    /// </summary>
    /// <param name="x">Screen X coordinate.</param>
    /// <param name="y">Screen Y coordinate.</param>
    public static void WarpCursor(int x, int y)
    {
        if (OperatingSystem.IsWindows())
            WindowsCursor.SetCursorPos(x, y);
        else if (OperatingSystem.IsLinux())
            LinuxCursor.WarpCursor(x, y);
        else if (OperatingSystem.IsMacOS())
            MacCursor.WarpCursor(x, y);
    }

    /// <summary>
    /// Windows cursor warping via user32.dll SetCursorPos.
    /// </summary>
    private static class WindowsCursor
    {
        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern uint SetCursorPos(int x, int y);
    }

    /// <summary>
    /// Linux (X11) cursor warping via libX11.so XWarpPointer.
    /// Note: This only works on Xorg; Wayland does not support global cursor warping.
    /// On Wayland, relative mouse mode should use zwp_relative_pointer_v1 protocol instead.
    /// </summary>
    private static class LinuxCursor
    {
        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XWarpPointer(IntPtr display, IntPtr srcWindow, IntPtr destWindow,
            int srcX, int srcY, uint srcWidth, uint srcHeight, int destX, int destY);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        public static void WarpCursor(int x, int y)
        {
            IntPtr display = IntPtr.Zero;
            try
            {
                display = XOpenDisplay(IntPtr.Zero);
                if (display == IntPtr.Zero)
                    return; // X11 not available (likely Wayland-only)

                var rootWindow = XDefaultRootWindow(display);
                XWarpPointer(display, IntPtr.Zero, rootWindow, 0, 0, 0, 0, x, y);
                XFlush(display);
            }
            finally
            {
                if (display != IntPtr.Zero)
                    XCloseDisplay(display);
            }
        }
    }

    /// <summary>
    /// macOS cursor warping via CoreGraphics CGWarpMouseCursorPosition.
    /// </summary>
    private static class MacCursor
    {
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGWarpMouseCursorPosition(CGPoint point);

        [StructLayout(LayoutKind.Sequential)]
        private struct CGPoint
        {
            public double X;
            public double Y;
        }

        public static void WarpCursor(int x, int y)
        {
            CGWarpMouseCursorPosition(new CGPoint { X = x, Y = y });
        }
    }
}
