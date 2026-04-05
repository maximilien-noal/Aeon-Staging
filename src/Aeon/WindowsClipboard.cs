using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Media.Imaging;

namespace Aeon.Emulator.Launcher;

[SupportedOSPlatform("windows")]
internal static partial class WindowsClipboard
{
    private const uint CF_DIB = 8;
    private const uint GMEM_MOVEABLE = 0x0002;
}

