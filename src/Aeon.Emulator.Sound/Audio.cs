using Spice86.Audio.Backend.Audio;
using Spice86.Audio.Filters;

namespace Aeon.Emulator.Sound;

internal static class Audio
{
    private static readonly AudioPlayerFactory Factory = new(AudioEngine.CrossPlatform);

    /// <summary>
    /// Creates a new audio player with standard settings.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz (default 44100).</param>
    /// <param name="framesPerBuffer">Frames per buffer (0 = default).</param>
    /// <returns>A started <see cref="AudioPlayer"/> instance ready for writing.</returns>
    public static AudioPlayer CreatePlayer(int sampleRate = 44100, int framesPerBuffer = 0)
    {
        var player = Factory.CreatePlayer(sampleRate, framesPerBuffer, prebufferMs: 0, allowNegotiate: true);
        player.Start();
        return player;
    }

    /// <summary>
    /// Writes all data in the buffer to the player, blocking until complete.
    /// </summary>
    public static void WriteFullBuffer(AudioPlayer player, Span<float> buffer)
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

    /// <summary>
    /// Converts short samples to float and writes all data to the player, blocking until complete.
    /// </summary>
    public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<short> buffer)
    {
        // Convert short samples to float (interleaved stereo or mono)
        Span<float> floatBuffer = buffer.Length <= 4096
            ? stackalloc float[buffer.Length]
            : new float[buffer.Length];

        for (int i = 0; i < buffer.Length; i++)
            floatBuffer[i] = buffer[i] / 32768f;

        WriteFullBuffer(player, floatBuffer);
    }

    /// <summary>
    /// Converts unsigned byte samples to float and writes all data to the player, blocking until complete.
    /// </summary>
    public static void WriteFullBuffer(AudioPlayer player, ReadOnlySpan<byte> buffer)
    {
        // Convert unsigned 8-bit PCM to float (centered at 0)
        Span<float> floatBuffer = buffer.Length <= 4096
            ? stackalloc float[buffer.Length]
            : new float[buffer.Length];

        for (int i = 0; i < buffer.Length; i++)
            floatBuffer[i] = (buffer[i] - 128) / 128f;

        WriteFullBuffer(player, floatBuffer);
    }
}
