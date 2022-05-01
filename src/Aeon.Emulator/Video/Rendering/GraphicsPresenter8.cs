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
        public GraphicsPresenter8(VideoMode videoMode) : base(videoMode)
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
            /*
            for (int i = 0; i < totalPixels; i++)
            {
                var value = srcPtr[i];
                destPtr[i] = palette[value];
            }
            */
            //RowBytes=2560
            //Pixelize=640x400
            var height = this.VideoMode.Height;
            var width = this.VideoMode.Width;
            var offset = 0;
            for (int row = 0; row < height; row++)
            {
                uint* startOfLine = destPtr;
                uint* endOfLine = destPtr + width;
                for (uint* column = startOfLine; column < endOfLine; column++)
                {
                    var src = srcPtr[offset];
                    var pixel = palette[src];
                    *column = ToArgb(pixel);
                    offset++;
                }
                destPtr += width;
            }
        }
        public uint ToRgba(uint pixel) {
            var color = Color.FromArgb((int)pixel);
            return (uint)(color.R << 16 | color.G << 8 | color.B) | 0xFF000000;
        }

        public uint ToBgra(uint pixel) {
            var color = Color.FromArgb((int)pixel);
            return (uint)(color.B << 16 | color.G << 8 | color.R) | 0xFF000000;
        }

        public uint ToArgb(uint pixel) {
            var color = Color.FromArgb((int)pixel);
            return 0xFF000000 | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }
    }
}
