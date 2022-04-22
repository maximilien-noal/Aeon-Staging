using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Aeon.Emulator.Launcher
{
    [SupportedOSPlatform("windows")]
    internal static class NativeMethods
    {
        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern uint SetCursorPos(int x, int y);
    }
}
