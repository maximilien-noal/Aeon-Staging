using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media.Imaging;

namespace Aeon.Emulator.Launcher;

[SupportedOSPlatform("windows")]
internal static partial class WindowsClipboard
{
    private const uint CF_DIB = 8;
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    public static unsafe void SetBitmap(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        int width = size.Width;
        int height = size.Height;
        int stride = width * 4;
        int pixelDataSize = stride * height;

        byte[] pixels = new byte[pixelDataSize];
        fixed (byte* ptr = pixels)
        {
            bitmap.CopyPixels(new PixelRect(size), (nint)ptr, pixelDataSize, stride);
        }

        byte[] flipped = new byte[pixelDataSize];
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(pixels, y * stride, flipped, (height - 1 - y) * stride, stride);
        }

        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = width,
            biHeight = height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0,
            biSizeImage = (uint)pixelDataSize,
            biXPelsPerMeter = 0,
            biYPelsPerMeter = 0,
            biClrUsed = 0,
            biClrImportant = 0
        };

        int totalSize = sizeof(BITMAPINFOHEADER) + pixelDataSize;
        nint hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)totalSize);
        if (hGlobal == 0)
            return;

        nint locked = GlobalLock(hGlobal);
        if (locked == 0)
            return;

        *(BITMAPINFOHEADER*)locked = header;
        fixed (byte* src = flipped)
        {
            Buffer.MemoryCopy(src, (void*)(locked + sizeof(BITMAPINFOHEADER)), pixelDataSize, pixelDataSize);
        }

        GlobalUnlock(hGlobal);

        if (OpenClipboard(0))
        {
            EmptyClipboard();
            SetClipboardData(CF_DIB, hGlobal);
            CloseClipboard();
        }
    }
}

