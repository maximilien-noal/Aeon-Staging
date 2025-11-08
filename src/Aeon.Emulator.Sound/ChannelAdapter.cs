using System;

namespace Aeon.Emulator.Sound
{
    /// <summary>
    /// Helper class for audio channel conversion.
    /// </summary>
    internal static class ChannelAdapter
    {
        /// <summary>
        /// Converts mono audio to stereo by duplicating each sample.
        /// </summary>
        /// <param name="mono">Mono audio samples.</param>
        /// <param name="stereo">Stereo output buffer (must be at least 2x mono length).</param>
        public static void MonoToStereo(ReadOnlySpan<float> mono, Span<float> stereo)
        {
            for (int i = 0; i < mono.Length; i++)
            {
                stereo[i * 2] = mono[i];
                stereo[i * 2 + 1] = mono[i];
            }
        }

        /// <summary>
        /// Converts mono audio to stereo by duplicating each sample.
        /// </summary>
        /// <param name="mono">Mono audio samples (bytes).</param>
        /// <param name="stereo">Stereo output buffer (must be at least 2x mono length).</param>
        public static void MonoToStereo(ReadOnlySpan<byte> mono, Span<byte> stereo)
        {
            for (int i = 0; i < mono.Length; i++)
            {
                stereo[i * 2] = mono[i];
                stereo[i * 2 + 1] = mono[i];
            }
        }
    }
}
