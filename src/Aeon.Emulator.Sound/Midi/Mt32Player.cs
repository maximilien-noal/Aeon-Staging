using System.IO.Compression;
using Mt32emu;
using Spice86.Audio.Backend.Audio;

namespace Aeon.Emulator.Sound;

internal sealed class Mt32Player : IDisposable
{
    private const int SampleRate = 44100;
    private const int BufferSize = 11025; // ~250ms at 44100Hz

    private readonly Mt32Context context = new();
    private readonly AudioPlayer audioPlayer = Audio.CreatePlayer(SampleRate);
    private readonly Thread playbackThread;
    private volatile bool playing;
    private bool disposed;

    public Mt32Player(string romsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsPath);

        this.LoadRoms(romsPath);

        var analogMode = Mt32GlobalState.GetBestAnalogOutputMode(SampleRate);
        this.context.AnalogOutputMode = analogMode;
        this.context.SetSampleRate(SampleRate);

        this.context.OpenSynth();
        this.playing = true;
        this.playbackThread = new Thread(this.PlaybackLoop) { IsBackground = true };
        this.playbackThread.Start();
    }

    public void PlayShortMessage(uint message) => this.context.PlayMessage(message);
    public void PlaySysex(ReadOnlySpan<byte> data) => this.context.PlaySysex(data);
    public void Pause() => this.playing = false;
    public void Resume() => this.playing = true;
    public void Dispose()
    {
        if (!this.disposed)
        {
            this.disposed = true;
            this.playing = false;
            this.playbackThread.Join();
            this.context.Dispose();
            this.audioPlayer.Dispose();
        }
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

            try
            {
                this.context.Render(buffer);
                Audio.WriteFullBuffer(this.audioPlayer, buffer);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private void LoadRoms(string path)
    {
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith(".ROM", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = entry.Open();
                    this.context.AddRom(stream);
                }
            }
        }
        else if (Directory.Exists(path))
        {
            foreach (var fileName in Directory.EnumerateFiles(path, "*.ROM"))
                this.context.AddRom(fileName);
        }
    }
}
