using MeltySynth;
using Spice86.Audio.Backend.Audio;

namespace Aeon.Emulator.Sound;

internal sealed class MeltySynthMidiMapper : MidiDevice
{
    private const int SampleRate = 44100;
    private const int BufferSize = 11025; // ~250ms at 44100Hz

    private readonly Synthesizer synthesizer;
    private readonly AudioPlayer audioPlayer;
    private readonly Thread playbackThread;
    private volatile bool playing;
    private bool disposed;

    public MeltySynthMidiMapper(string soundFontPath)
    {
        if (string.IsNullOrEmpty(soundFontPath))
            throw new ArgumentNullException(nameof(soundFontPath));

        this.audioPlayer = Audio.CreatePlayer(SampleRate);
        this.synthesizer = new Synthesizer(soundFontPath, SampleRate);
        this.playing = true;
        this.playbackThread = new Thread(this.PlaybackLoop) { IsBackground = true };
        this.playbackThread.Start();
    }

    public override void Pause()
    {
        this.playing = false;
    }
    public override void Resume()
    {
        this.playing = true;
    }

    protected override void PlayShortMessage(uint message)
    {
        this.synthesizer.ProcessMidiMessage((int)message & 0xF, (int)message & 0xF0, (byte)(message >>> 8), (byte)(message >>> 16));
    }
    protected override void PlaySysex(ReadOnlySpan<byte> data)
    {
    }
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            this.disposed = true;
            this.playing = false;
            if (disposing)
            {
                this.playbackThread.Join();
                this.audioPlayer.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void PlaybackLoop()
    {
        Span<float> buffer = stackalloc float[BufferSize];

        while (!this.disposed)
        {
            if (!this.playing)
            {
                Thread.Sleep(1);
                continue;
            }

            this.synthesizer.RenderInterleaved(buffer);
            Audio.WriteFullBuffer(this.audioPlayer, buffer);
        }
    }
}
