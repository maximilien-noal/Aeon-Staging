using System;
using System.Threading;
using Ownaudio.Core;

namespace Aeon.Emulator.Sound
{
    internal static class Audio
    {
        public static IAudioEngine CreatePlayer(bool useCallback = false)
        {
            var engine = AudioEngineFactory.CreateDefault();
            engine.Initialize(AudioConfig.Default);
            engine.Start();
            return engine;
        }

        public static void WriteFullBuffer(IAudioEngine player, ReadOnlySpan<float> buffer)
        {
            player.Send(buffer);
        }
        public static void WriteFullBuffer(IAudioEngine player, ReadOnlySpan<short> buffer)
        {
            // Convert short to float
            Span<float> floatBuffer = stackalloc float[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                floatBuffer[i] = buffer[i] / 32768f;
            }
            player.Send(floatBuffer);
        }
        public static void WriteFullBuffer(IAudioEngine player, ReadOnlySpan<byte> buffer)
        {
            // Convert byte to float
            Span<float> floatBuffer = stackalloc float[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                floatBuffer[i] = (buffer[i] - 128) / 128f;
            }
            player.Send(floatBuffer);
        }
    }
}
