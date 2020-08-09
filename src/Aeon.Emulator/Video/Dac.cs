﻿using System;

namespace Aeon.Emulator.Video
{
    /// <summary>
    /// Emulates the VGA DAC which provides access to the palette.
    /// </summary>
    internal sealed class Dac
    {
        private readonly unsafe uint* palette;
        private readonly UnsafeBuffer<uint> paletteBuffer = new UnsafeBuffer<uint>(256);
        private int readChannel;
        private int writeChannel;
        private byte readIndex;
        private byte writeIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dac"/> class.
        /// </summary>
        public Dac()
        {
            unsafe
            {
                this.palette = this.paletteBuffer.ToPointer();
            }

            this.Reset();
        }

        /// <summary>
        /// Gets the full 256-color palette.
        /// </summary>
        public ReadOnlySpan<uint> Palette
        {
            get
            {
                unsafe
                {
                    return new ReadOnlySpan<uint>(this.palette, 256);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current palette read index.
        /// </summary>
        public byte ReadIndex
        {
            get => this.readIndex;
            set
            {
                this.readIndex = value;
                this.readChannel = 0;
            }
        }
        /// <summary>
        /// Gets or sets the current palette write index.
        /// </summary>
        public byte WriteIndex
        {
            get => this.writeIndex;
            set
            {
                this.writeIndex = value;
                this.writeChannel = 0;
            }
        }

        /// <summary>
        /// Reads the next channel in the current color.
        /// </summary>
        /// <returns>Red, green, or blue channel value.</returns>
        public byte Read()
        {
            unsafe
            {
                uint color = this.palette[this.readIndex];
                this.readChannel++;
                if (this.readChannel == 1)
                {
                    return (byte)((color >> 18) & 0x3F);
                }
                else if (this.readChannel == 2)
                {
                    return (byte)((color >> 10) & 0x3F);
                }
                else
                {
                    this.readChannel = 0;
                    this.readIndex++;
                    return (byte)((color >> 2) & 0x3F);
                }
            }
        }
        /// <summary>
        /// Writes the next channel in the current color.
        /// </summary>
        /// <param name="value">Red, green, or blue channel value.</param>
        public void Write(byte value)
        {
            unsafe
            {
                this.writeChannel++;
                if (this.writeChannel == 1)
                {
                    this.palette[this.writeIndex] &= 0xFF00FFFF;
                    this.palette[this.writeIndex] |= (uint)((value & 0x3F) << 18);
                }
                else if (this.writeChannel == 2)
                {
                    this.palette[this.writeIndex] &= 0xFFFF00FF;
                    this.palette[this.writeIndex] |= (uint)((value & 0x3F) << 10);
                }
                else
                {
                    this.palette[this.writeIndex] &= 0xFFFFFF00;
                    this.palette[this.writeIndex] |= (uint)((value & 0x3F) << 2);
                    this.writeChannel = 0;
                    this.writeIndex++;
                }
            }
        }
        /// <summary>
        /// Resets the colors to the default 256-color VGA palette.
        /// </summary>
        public void Reset()
        {
            var source = DefaultPalette;
            for (int i = 0; i < 256; i++)
            {
                uint r = source[i * 3];
                uint g = source[i * 3 + 1];
                uint b = source[i * 3 + 2];
                unsafe
                {
                    this.palette[i] = b | (g << 8) | (r << 16);
                }
            }
        }
        /// <summary>
        /// Sets a color to the specified RGB values.
        /// </summary>
        /// <param name="index">Index of color to set.</param>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        public void SetColor(byte index, byte r, byte g, byte b)
        {
            uint red = (r & 0x3Fu) << 18;
            uint green = (g & 0x3Fu) << 10;
            uint blue = (b & 0x3Fu) << 2;

            unsafe
            {
                this.palette[index] = red | green | blue;
            }
        }

        #region DefaultPalette
        private static ReadOnlySpan<byte> DefaultPalette => new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0xA8, 0x00, 0xA8, 0x00, 0x00, 0xA8, 0xA8, 0xA8, 0x00, 0x00, 0xA8,
                0x00, 0xA8, 0xA8, 0x54, 0x00, 0xA8, 0xA8, 0xA8, 0x54, 0x54, 0x54, 0x54, 0x54, 0xFC, 0x54, 0xFC,
                0x54, 0x54, 0xFC, 0xFC, 0xFC, 0x54, 0x54, 0xFC, 0x54, 0xFC, 0xFC, 0xFC, 0x54, 0xFC, 0xFC, 0xFC,
                0x00, 0x00, 0x00, 0x14, 0x14, 0x14, 0x20, 0x20, 0x20, 0x2C, 0x2C, 0x2C, 0x38, 0x38, 0x38, 0x44,
                0x44, 0x44, 0x50, 0x50, 0x50, 0x60, 0x60, 0x60, 0x70, 0x70, 0x70, 0x80, 0x80, 0x80, 0x90, 0x90,
                0x90, 0xA0, 0xA0, 0xA0, 0xB4, 0xB4, 0xB4, 0xC8, 0xC8, 0xC8, 0xE0, 0xE0, 0xE0, 0xFC, 0xFC, 0xFC,
                0x00, 0x00, 0xFC, 0x40, 0x00, 0xFC, 0x7C, 0x00, 0xFC, 0xBC, 0x00, 0xFC, 0xFC, 0x00, 0xFC, 0xFC,
                0x00, 0xBC, 0xFC, 0x00, 0x7C, 0xFC, 0x00, 0x40, 0xFC, 0x00, 0x00, 0xFC, 0x40, 0x00, 0xFC, 0x7C,
                0x00, 0xFC, 0xBC, 0x00, 0xFC, 0xFC, 0x00, 0xBC, 0xFC, 0x00, 0x7C, 0xFC, 0x00, 0x40, 0xFC, 0x00,
                0x00, 0xFC, 0x00, 0x00, 0xFC, 0x40, 0x00, 0xFC, 0x7C, 0x00, 0xFC, 0xBC, 0x00, 0xFC, 0xFC, 0x00,
                0xBC, 0xFC, 0x00, 0x7C, 0xFC, 0x00, 0x40, 0xFC, 0x7C, 0x7C, 0xFC, 0x9C, 0x7C, 0xFC, 0xBC, 0x7C,
                0xFC, 0xDC, 0x7C, 0xFC, 0xFC, 0x7C, 0xFC, 0xFC, 0x7C, 0xDC, 0xFC, 0x7C, 0xBC, 0xFC, 0x7C, 0x9C,
                0xFC, 0x7C, 0x7C, 0xFC, 0x9C, 0x7C, 0xFC, 0xBC, 0x7C, 0xFC, 0xDC, 0x7C, 0xFC, 0xFC, 0x7C, 0xDC,
                0xFC, 0x7C, 0xBC, 0xFC, 0x7C, 0x9C, 0xFC, 0x7C, 0x7C, 0xFC, 0x7C, 0x7C, 0xFC, 0x9C, 0x7C, 0xFC,
                0xBC, 0x7C, 0xFC, 0xDC, 0x7C, 0xFC, 0xFC, 0x7C, 0xDC, 0xFC, 0x7C, 0xBC, 0xFC, 0x7C, 0x9C, 0xFC,
                0xB4, 0xB4, 0xFC, 0xC4, 0xB4, 0xFC, 0xD8, 0xB4, 0xFC, 0xE8, 0xB4, 0xFC, 0xFC, 0xB4, 0xFC, 0xFC,
                0xB4, 0xE8, 0xFC, 0xB4, 0xD8, 0xFC, 0xB4, 0xC4, 0xFC, 0xB4, 0xB4, 0xFC, 0xC4, 0xB4, 0xFC, 0xD8,
                0xB4, 0xFC, 0xE8, 0xB4, 0xFC, 0xFC, 0xB4, 0xE8, 0xFC, 0xB4, 0xD8, 0xFC, 0xB4, 0xC4, 0xFC, 0xB4,
                0xB4, 0xFC, 0xB4, 0xB4, 0xFC, 0xC4, 0xB4, 0xFC, 0xD8, 0xB4, 0xFC, 0xE8, 0xB4, 0xFC, 0xFC, 0xB4,
                0xE8, 0xFC, 0xB4, 0xD8, 0xFC, 0xB4, 0xC4, 0xFC, 0x00, 0x00, 0x70, 0x1C, 0x00, 0x70, 0x38, 0x00,
                0x70, 0x54, 0x00, 0x70, 0x70, 0x00, 0x70, 0x70, 0x00, 0x54, 0x70, 0x00, 0x38, 0x70, 0x00, 0x1C,
                0x70, 0x00, 0x00, 0x70, 0x1C, 0x00, 0x70, 0x38, 0x00, 0x70, 0x54, 0x00, 0x70, 0x70, 0x00, 0x54,
                0x70, 0x00, 0x38, 0x70, 0x00, 0x1C, 0x70, 0x00, 0x00, 0x70, 0x00, 0x00, 0x70, 0x1C, 0x00, 0x70,
                0x38, 0x00, 0x70, 0x54, 0x00, 0x70, 0x70, 0x00, 0x54, 0x70, 0x00, 0x38, 0x70, 0x00, 0x1C, 0x70,
                0x38, 0x38, 0x70, 0x44, 0x38, 0x70, 0x54, 0x38, 0x70, 0x60, 0x38, 0x70, 0x70, 0x38, 0x70, 0x70,
                0x38, 0x60, 0x70, 0x38, 0x54, 0x70, 0x38, 0x44, 0x70, 0x38, 0x38, 0x70, 0x44, 0x38, 0x70, 0x54,
                0x38, 0x70, 0x60, 0x38, 0x70, 0x70, 0x38, 0x60, 0x70, 0x38, 0x54, 0x70, 0x38, 0x44, 0x70, 0x38,
                0x38, 0x70, 0x38, 0x38, 0x70, 0x44, 0x38, 0x70, 0x54, 0x38, 0x70, 0x60, 0x38, 0x70, 0x70, 0x38,
                0x60, 0x70, 0x38, 0x54, 0x70, 0x38, 0x44, 0x70, 0x50, 0x50, 0x70, 0x58, 0x50, 0x70, 0x60, 0x50,
                0x70, 0x68, 0x50, 0x70, 0x70, 0x50, 0x70, 0x70, 0x50, 0x68, 0x70, 0x50, 0x60, 0x70, 0x50, 0x58,
                0x70, 0x50, 0x50, 0x70, 0x58, 0x50, 0x70, 0x60, 0x50, 0x70, 0x68, 0x50, 0x70, 0x70, 0x50, 0x68,
                0x70, 0x50, 0x60, 0x70, 0x50, 0x58, 0x70, 0x50, 0x50, 0x70, 0x50, 0x50, 0x70, 0x58, 0x50, 0x70,
                0x60, 0x50, 0x70, 0x68, 0x50, 0x70, 0x70, 0x50, 0x68, 0x70, 0x50, 0x60, 0x70, 0x50, 0x58, 0x70,
                0x00, 0x00, 0x40, 0x10, 0x00, 0x40, 0x20, 0x00, 0x40, 0x30, 0x00, 0x40, 0x40, 0x00, 0x40, 0x40,
                0x00, 0x30, 0x40, 0x00, 0x20, 0x40, 0x00, 0x10, 0x40, 0x00, 0x00, 0x40, 0x10, 0x00, 0x40, 0x20,
                0x00, 0x40, 0x30, 0x00, 0x40, 0x40, 0x00, 0x30, 0x40, 0x00, 0x20, 0x40, 0x00, 0x10, 0x40, 0x00,
                0x00, 0x40, 0x00, 0x00, 0x40, 0x10, 0x00, 0x40, 0x20, 0x00, 0x40, 0x30, 0x00, 0x40, 0x40, 0x00,
                0x30, 0x40, 0x00, 0x20, 0x40, 0x00, 0x10, 0x40, 0x20, 0x20, 0x40, 0x28, 0x20, 0x40, 0x30, 0x20,
                0x40, 0x38, 0x20, 0x40, 0x40, 0x20, 0x40, 0x40, 0x20, 0x38, 0x40, 0x20, 0x30, 0x40, 0x20, 0x28,
                0x40, 0x20, 0x20, 0x40, 0x28, 0x20, 0x40, 0x30, 0x20, 0x40, 0x38, 0x20, 0x40, 0x40, 0x20, 0x38,
                0x40, 0x20, 0x30, 0x40, 0x20, 0x28, 0x40, 0x20, 0x20, 0x40, 0x20, 0x20, 0x40, 0x28, 0x20, 0x40,
                0x30, 0x20, 0x40, 0x38, 0x20, 0x40, 0x40, 0x20, 0x38, 0x40, 0x20, 0x30, 0x40, 0x20, 0x28, 0x40,
                0x2C, 0x2C, 0x40, 0x30, 0x2C, 0x40, 0x34, 0x2C, 0x40, 0x3C, 0x2C, 0x40, 0x40, 0x2C, 0x40, 0x40,
                0x2C, 0x3C, 0x40, 0x2C, 0x34, 0x40, 0x2C, 0x30, 0x40, 0x2C, 0x2C, 0x40, 0x30, 0x2C, 0x40, 0x34,
                0x2C, 0x40, 0x3C, 0x2C, 0x40, 0x40, 0x2C, 0x3C, 0x40, 0x2C, 0x34, 0x40, 0x2C, 0x30, 0x40, 0x2C,
                0x2C, 0x40, 0x2C, 0x2C, 0x40, 0x30, 0x2C, 0x40, 0x34, 0x2C, 0x40, 0x3C, 0x2C, 0x40, 0x40, 0x2C,
                0x3C, 0x40, 0x2C, 0x34, 0x40, 0x2C, 0x30, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
        #endregion
    }
}
