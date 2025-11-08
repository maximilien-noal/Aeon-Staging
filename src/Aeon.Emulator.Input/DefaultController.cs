using System;
using System.Diagnostics;
using Silk.NET.SDL;

namespace Aeon.Emulator.Input
{
    internal sealed class DefaultController : IGameController
    {
        private static readonly Lazy<Sdl> sdlInstance = new Lazy<Sdl>(InitializeSdl);
        private IGameController current;
        private readonly Stopwatch lastAttempt = new();

        public string Name => this.current?.Name ?? "No Controller";

        public bool TryGetState(out GameControllerState state)
        {
            if (this.current == null || !this.current.TryGetState(out state))
            {
                this.current?.Dispose();
                this.current = null;

                if (!this.lastAttempt.IsRunning || this.lastAttempt.Elapsed >= new TimeSpan(0, 0, 5))
                {
                    this.current = GetDefaultController();
                    this.lastAttempt.Restart();

                    if (this.current != null)
                        return this.current.TryGetState(out state);
                }

                state = default;
                return false;
            }

            return true;
        }

        public void Dispose() => this.current?.Dispose();

        private static Sdl InitializeSdl()
        {
            var sdl = Sdl.GetApi();
            unsafe
            {
                // Initialize SDL joystick and game controller subsystems
                if (sdl.Init(Sdl.InitJoystick | Sdl.InitGamecontroller) < 0)
                {
                    throw new InvalidOperationException($"Failed to initialize SDL: {System.Runtime.InteropServices.Marshal.PtrToStringUTF8((IntPtr)sdl.GetError())}");
                }
            }
            return sdl;
        }

        private static IGameController GetDefaultController()
        {
            try
            {
                var sdl = sdlInstance.Value;

                unsafe
                {
                    // Update SDL events to detect newly connected controllers
                    sdl.GameControllerUpdate();

                    // Find the first available game controller
                    int numJoysticks = sdl.NumJoysticks();
                    for (int i = 0; i < numJoysticks; i++)
                    {
                        var isGameController = sdl.IsGameController(i);
                        if (isGameController == SdlBool.True)
                        {
                            var controller = sdl.GameControllerOpen(i);
                            if (controller != null)
                            {
                                var joystick = sdl.GameControllerGetJoystick(controller);
                                var instanceId = sdl.JoystickInstanceID(joystick);
                                return new SdlGameController(sdl, controller, instanceId);
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
