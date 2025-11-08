#if !WINDOWS
using System;
using Silk.NET.SDL;

namespace Aeon.Emulator.Input
{
    /// <summary>
    /// Cross-platform game controller implementation using SDL2.
    /// </summary>
    internal sealed class SdlGameController : IGameController
    {
        private readonly Sdl sdl;
        private readonly unsafe GameController* controller;
        private readonly int instanceId;
        private bool disposed;

        public unsafe SdlGameController(Sdl sdl, GameController* controller, int instanceId)
        {
            this.sdl = sdl;
            this.controller = controller;
            this.instanceId = instanceId;
            this.Name = GetControllerName(sdl, controller);
        }

        public string Name { get; }

        public bool TryGetState(out GameControllerState state)
        {
            if (disposed)
            {
                state = default;
                return false;
            }

            unsafe
            {
                // Check if controller is still attached
                var attached = sdl.GameControllerGetAttached(controller);
                if (attached == SdlBool.False)
                {
                    state = default;
                    return false;
                }

                // Get button states
                var buttons = GameControllerButtons.None;
                if (sdl.GameControllerGetButton(controller, GameControllerButton.A) != 0)
                    buttons |= GameControllerButtons.Button1;
                if (sdl.GameControllerGetButton(controller, GameControllerButton.B) != 0)
                    buttons |= GameControllerButtons.Button2;
                if (sdl.GameControllerGetButton(controller, GameControllerButton.X) != 0)
                    buttons |= GameControllerButtons.Button3;
                if (sdl.GameControllerGetButton(controller, GameControllerButton.Y) != 0)
                    buttons |= GameControllerButtons.Button4;

                // Get axis positions
                float xAxis = NormalizeAxis(sdl.GameControllerGetAxis(controller, GameControllerAxis.Leftx));
                float yAxis = NormalizeAxis(sdl.GameControllerGetAxis(controller, GameControllerAxis.Lefty));

                // Check D-pad
                if (sdl.GameControllerGetButton(controller, GameControllerButton.DpadLeft) != 0)
                    xAxis = -1;
                else if (sdl.GameControllerGetButton(controller, GameControllerButton.DpadRight) != 0)
                    xAxis = 1;

                if (sdl.GameControllerGetButton(controller, GameControllerButton.DpadUp) != 0)
                    yAxis = -1;
                else if (sdl.GameControllerGetButton(controller, GameControllerButton.DpadDown) != 0)
                    yAxis = 1;

                // Apply deadzone
                if (MathF.Abs(xAxis) < 0.25f)
                    xAxis = 0;
                if (MathF.Abs(yAxis) < 0.25f)
                    yAxis = 0;

                state = new GameControllerState(xAxis, yAxis, buttons);
                return true;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                unsafe
                {
                    sdl.GameControllerClose(controller);
                }
            }
        }

        private static float NormalizeAxis(short value)
        {
            return value / (float)short.MaxValue;
        }

        private static unsafe string GetControllerName(Sdl sdl, GameController* controller)
        {
            var namePtr = sdl.GameControllerName(controller);
            if (namePtr != null)
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringUTF8((IntPtr)namePtr) ?? "SDL Game Controller";
            }
            return "SDL Game Controller";
        }
    }
}
#endif
