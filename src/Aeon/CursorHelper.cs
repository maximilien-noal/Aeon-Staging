using System.Runtime.InteropServices;

namespace Aeon.Emulator.Launcher;

internal static class CursorHelper
{
    public static void WarpCursor(int x, int y)
    {
        if (OperatingSystem.IsWindows())
            WindowsCursor.SetCursorPos(x, y);
        else if (OperatingSystem.IsLinux())
            LinuxCursor.WarpCursor(x, y);
        else if (OperatingSystem.IsMacOS())
            MacCursor.WarpCursor(x, y);
    }

    private static class WindowsCursor
    {
        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern uint SetCursorPos(int x, int y);
    }

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
                    return;

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
