using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Aeon.Emulator.Input
{
    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct XINPUT_STATE
    {
        public readonly uint dwPacketNumber;
        public readonly XInputGamepadState Gamepad;
    }
}
