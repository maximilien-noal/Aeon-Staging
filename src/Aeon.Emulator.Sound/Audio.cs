using System;
using System.Runtime.Versioning;
using System.Threading;
using TinyAudio;

namespace Aeon.Emulator.Sound
{
    [SupportedOSPlatform("windows")]
    internal static class Audio
    {
        public static AudioPlayer? CreatePlayer(bool useCallback = false)
        {
            if (OperatingSystem.IsWindows())
            {
                return WasapiAudioPlayer.Create(TimeSpan.FromSeconds(0.25), useCallback);
            }

            return null;
        }

        public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<float> buffer)
        {
            var writeBuffer = buffer;

            while (true)
            {
                int count = player.WriteData(writeBuffer);
                writeBuffer = writeBuffer[count..];
                if (writeBuffer.IsEmpty)
                    return;

                Thread.Sleep(1);
            }
        }
        public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<short> buffer)
        {
            var writeBuffer = buffer;

            while (true)
            {
                int count = player.WriteData(writeBuffer);
                writeBuffer = writeBuffer[count..];
                if (writeBuffer.IsEmpty)
                    return;

                Thread.Sleep(1);
            }
        }
        public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<byte> buffer)
        {
            var writeBuffer = buffer;

            while (true)
            {
                int count = player.WriteData(writeBuffer);
                writeBuffer = writeBuffer[count..];
                if (writeBuffer.IsEmpty)
                    return;

                Thread.Sleep(1);
            }
        }
    }
}
