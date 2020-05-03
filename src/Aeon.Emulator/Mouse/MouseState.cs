﻿using System;

namespace Aeon.Emulator.Mouse
{
    internal struct MouseState
    {
        public int X;
        public int Y;
        public MouseButtons PressedButtons;
    }

    [Flags]
    internal enum CallbackMask
    {
        Disabled = 0,
        Move = 1,
        LeftButtonDown = 2,
        LeftButtonUp = 4,
        RightButtonDown = 8,
        RightButtonUp = 16,
        MiddleButtonDown = 32,
        MiddleButtonUp = 64
    }
}
