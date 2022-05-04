using System;
using System.Drawing;

namespace Aeon.Emulator.Video.Rendering
{
    /// <summary>
    /// Renders 8-bit graphics to a bitmap.
    /// </summary>
    public sealed class GraphicsPresenter8 : Presenter
    {
        /// <summary>
        /// Initializes a new instance of the GraphicsPresenter8 class.
        /// </summary>
        /// <param name="videoMode">VideoMode instance describing the video mode.</param>
        public unsafe GraphicsPresenter8(VideoMode videoMode, Func<uint, uint>? colorConversionFunc = null) : base(videoMode, colorConversionFunc)
        {
        }

        /// <summary>
        /// Updates the bitmap to match the current state of the video RAM.
        /// </summary>
        protected override unsafe void DrawFrame(IntPtr destination)
        {
            uint totalPixels = (uint)this.VideoMode.Width * (uint)this.VideoMode.Height;
            var palette = this.VideoMode.Palette;
            byte* srcPtr = (byte*)this.VideoMode.VideoRam.ToPointer() + (uint)this.VideoMode.StartOffset;
            uint* destPtr = (uint*)destination.ToPointer();

            var height = this.VideoMode.Height;
            var width = this.VideoMode.Width;
            var offset = 0;
            for (int y = 0; y < height; y++)
            {
                uint* startPtr = destPtr + offset;
                uint* endPtr = destPtr + offset + width;
                for (uint* x = startPtr; x < endPtr; x++)
                {
                    var src = srcPtr[offset];
                    var pixel = palette[src];
                    *x = ToNativeColorFormat(pixel);
                    offset++;
                }
            }
        }
    }
}
