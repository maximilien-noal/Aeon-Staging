using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Cross-platform replacement for FastBitmap using Avalonia's WriteableBitmap.
/// </summary>
internal sealed class AvaloniaBitmap : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the AvaloniaBitmap class.
    /// </summary>
    /// <param name="width">Width of the bitmap in pixels.</param>
    /// <param name="height">Height of the bitmap in pixels.</param>
    public AvaloniaBitmap(int width, int height)
    {
        this.Bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        this.Width = width;
        this.Height = height;
    }

    /// <summary>
    /// Gets the WriteableBitmap instance.
    /// </summary>
    public WriteableBitmap Bitmap { get; }
    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public int Width { get; }
    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Locks the bitmap for writing and returns a span over the pixel buffer.
    /// The caller must call Unlock() when done.
    /// </summary>
    public unsafe Span<uint> Lock(out ILockedFramebuffer framebuffer)
    {
        framebuffer = this.Bitmap.Lock();
        return new Span<uint>(framebuffer.Address.ToPointer(), this.Width * this.Height);
    }

    /// <summary>
    /// Releases resources used by the bitmap.
    /// </summary>
    public void Dispose()
    {
        if (!this.disposed)
        {
            this.Bitmap.Dispose();
            this.disposed = true;
        }
    }
}
