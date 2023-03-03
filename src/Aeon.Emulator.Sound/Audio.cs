﻿using System;
using System.Runtime.Versioning;
using System.Threading;
using TinyAudio;
using TinyAudio.PortAudio;

namespace Aeon.Emulator.Sound
{
    internal static class Audio
    {
        public static AudioPlayer? CreatePlayer(bool useCallback = false)
        {
            if (OperatingSystem.IsWindows())
            {
                return WasapiAudioPlayer.Create(TimeSpan.FromSeconds(0.25), useCallback);
            }
            else
            {
                return PortAudioPlayer.Create(48000, 2048);
            }
        }

        public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<float> buffer)
        {
            var writeBuffer = buffer;

            while (true)
            {
                int count = (int)player.WriteData(writeBuffer);
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
                int count = (int)player.WriteData(writeBuffer);
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
                int count = (int)player.WriteData(writeBuffer);
                writeBuffer = writeBuffer[count..];
                if (writeBuffer.IsEmpty)
                    return;

                Thread.Sleep(1);
            }
        }
    }
}
