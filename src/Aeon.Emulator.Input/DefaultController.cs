using System;
using System.Diagnostics;
#if !WINDOWS
using Silk.NET.SDL;
#else
using System.Linq;
#endif

namespace Aeon.Emulator.Input
{
    internal sealed class DefaultController : IGameController
    {
#if !WINDOWS
        private static readonly Lazy<Sdl> sdlInstance = new Lazy<Sdl>(InitializeSdl);
#endif
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

#if !WINDOWS
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
#else
        // Windows-only implementation using DirectInput/XInput
        private static IGameController GetDefaultController()
        {
            // first check for an XInput compatible controller
            if (XInput.TryGetController(out var controller))
                return controller;

            IntPtr hwnd;

            using (var p = Process.GetCurrentProcess())
            {
                hwnd = p.MainWindowHandle;
            }

            var dinput = DirectInput.GetInstance(hwnd);

            // if none found, try for the first DirectInput device
            var d = dinput.GetDevices(DeviceClass.GameController, DeviceEnumFlags.All).FirstOrDefault();
            if (d != null)
                return new DirectInputGameController(dinput.CreateDevice(d.InstanceId));

            return null;
        }
#endif
    }
}
