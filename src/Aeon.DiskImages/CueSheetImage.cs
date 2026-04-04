using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Aeon.DiskImages.Iso9660;
using Aeon.Emulator;
using Aeon.Emulator.Dos;
using Aeon.Emulator.Dos.VirtualFileSystem;

namespace Aeon.DiskImages;

public sealed partial class CueSheetImage : IMappedDrive, IAudioCD, IRawSectorReader
{
    private const int RawSectorSize = 2352;

    private readonly Iso9660Disc? disc;
    private readonly string fileImagePath;
    private AudioTrackPlayer? player;

    private bool disposed;

    public CueSheetImage(string fileName)
    {
        using var reader = File.OpenText(fileName);
        var line = reader.ReadLine();
        if (line == null || FileRegex().Match(line) is not Match m || !m.Success)
            throw new ArgumentException($"{fileName} is not a valid cue sheet.");

        var binFileName = m.Groups[1].Value;

        var tracks = new List<TrackInfo>();
        var indexes = new List<TrackIndex>();

        int? currentTrackIndex = null;
        TrackFormat? currentTrackType = null;
        CDTimeSpan preGap = default;
        CDTimeSpan postGap = default;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = TrackRegex().Match(line);
            if (match.Success)
            {
                if (currentTrackIndex.HasValue)
                {
                    tracks.Add(new TrackInfo(currentTrackType.GetValueOrDefault(), [.. indexes], preGap, postGap));
                    indexes.Clear();
                    preGap = default;
                    postGap = default;
                }

                currentTrackIndex = int.Parse(match.Groups[1].ValueSpan);
                currentTrackType = ParseFormat(match.Groups[2].Value);
                continue;
            }

            match = GapRegex().Match(line);
            if (match.Success)
            {
                var pos = CDTimeSpan.Parse(match.Groups[2].ValueSpan);
                if (match.Groups[1].Value == "PRE")
                    preGap = pos;
                else
                    postGap = pos;

                continue;
            }

            match = IndexRegex().Match(line);
            if (match.Success)
            {
                int indexNumber = int.Parse(match.Groups[1].ValueSpan);
                var indexPosition = CDTimeSpan.Parse(match.Groups[2].ValueSpan);
                indexes.Add(new TrackIndex(indexNumber, indexPosition));
            }
        }

        if (currentTrackIndex.HasValue)
            tracks.Add(new TrackInfo(currentTrackType.GetValueOrDefault(), [.. indexes], preGap, postGap));

        this.Tracks = Array.AsReadOnly(tracks.ToArray());

        this.fileImagePath = Path.Combine(Path.GetDirectoryName(fileName)!, binFileName);

        if (tracks.Count < 1)
            throw new InvalidOperationException("Cuesheet has no tracks.");

        this.TotalSectors = (int)(new FileInfo(this.fileImagePath).Length / RawSectorSize);

        if (tracks[0].Format == TrackFormat.Mode1)
        {
            var fileStream = new FileStream(this.fileImagePath, new FileStreamOptions { Options = FileOptions.RandomAccess, Access = FileAccess.Read });
            try
            {
                int dataSectors;
                if (tracks.Count > 1)
                    dataSectors = tracks[1].Indexes[0].Position.TotalSectors;
                else
                    dataSectors = (int)(fileStream.Length / RawSectorSize);

                this.disc = new Iso9660Disc(new Mode1Stream(fileStream, dataSectors));
            }
            catch
            {
                fileStream?.Dispose();
                throw;
            }
        }
    }

    public string VolumeLabel => this.disc?.PrimaryVolumeDescriptor.VolumeIdentifier ?? string.Empty;

    long IMappedDrive.FreeSpace => 0;

    public IReadOnlyList<TrackInfo> Tracks { get; }

    public int PlaybackSector
    {
        get => (int)((this.player?.Position ?? 0) / RawSectorSize);
        set
        {
            this.EnsureAudioPlayer();
            this.player.Position = value * RawSectorSize;
        }
    }

    public bool Playing => this.player?.Playing ?? false;

    public int TotalSectors { get; }

    int IRawSectorReader.SectorSize => 2048;

    public void Dispose()
    {
        if (!this.disposed)
        {
            this.player?.Dispose();
            this.disc?.Dispose();
            this.disposed = true;
        }
    }

    public ErrorCodeResult<Stream> OpenRead(VirtualPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var entry = this.disc?.GetDirectoryEntry(path.Elements);
        if (entry == null)
            return ExtendedErrorCode.FileNotFound;

        return this.disc!.Open(entry);
    }
    public ErrorCodeResult<IEnumerable<VirtualFileInfo>> GetDirectory(VirtualPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var entry = this.disc?.GetDirectoryEntry(path.Elements);
        if (entry == null)
            return ExtendedErrorCode.PathNotFound;

        return new ErrorCodeResult<IEnumerable<VirtualFileInfo>>(entry.Children);
    }
    public ErrorCodeResult<VirtualFileInfo> GetFileInfo(VirtualPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var info = this.disc?.GetDirectoryEntry(path.Elements);
        if (info != null)
            return info;

        return ExtendedErrorCode.FileNotFound;
    }

    private static TrackFormat ParseFormat(string s)
    {
        return s switch
        {
            "MODE1" or "MODE1/2352" => TrackFormat.Mode1,
            "AUDIO" => TrackFormat.Audio,
            _ => throw new NotSupportedException($"Track format {s} not supported.")
        };
    }

    [MemberNotNull(nameof(player))]
    private void EnsureAudioPlayer()
    {
        this.player ??= new AudioTrackPlayer(this.fileImagePath)
        {
            Position = this.PlaybackSector * RawSectorSize
        };
    }

    [MemberNotNull(nameof(player))]
    public void Play(int? sectors = null)
    {
        this.EnsureAudioPlayer();
        this.player.StopPosition = sectors.HasValue ? (this.PlaybackSector + sectors.GetValueOrDefault()) * 2352 : -1;
        this.player.Start();
    }
    public void Stop()
    {
        this.player?.Stop();
    }

    void IRawSectorReader.ReadSectors(int startingSector, int sectorsToRead, Span<byte> buffer)
    {
        this.disc!.ReadRaw(startingSector, sectorsToRead, buffer);
    }

    private sealed class Mode1Stream(Stream baseStream, int sectors) : Stream
    {
        private const int DataSectorSize = 2048;
        private const int HeaderSize = 16;
        private const long StartOffset = 16 * RawSectorSize;
        private readonly Stream baseStream = baseStream;
        private readonly int sectors = sectors;
        private int currentSector;
        private int currentOffset;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => this.sectors * DataSectorSize;
        public override long Position
        {
            get => (this.currentSector * DataSectorSize) + this.currentOffset;
            set
            {
                var (sector, offset) = Math.DivRem(value, DataSectorSize);
                this.currentSector = (int)sector;
                this.currentOffset = (int)offset;
            }
        }
        public override int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
                return 0;

            int totalBytesRead = 0;
            var currentBuffer = buffer;

            while (!currentBuffer.IsEmpty)
            {
                int bytesRead = this.ReadInternal(currentBuffer);
                if (bytesRead == 0)
                    break;
                totalBytesRead += bytesRead;
                currentBuffer = currentBuffer[bytesRead..];
            }

            return totalBytesRead;
        }
        public override int Read(byte[] buffer, int offset, int count) => this.Read(buffer.AsSpan(offset, count));
        public override int ReadByte()
        {
            Span<byte> oneByte = stackalloc byte[1];
            return this.Read(oneByte) == 1 ? oneByte[0] : -1;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => this.Position + offset,
                SeekOrigin.End => this.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
        }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.baseStream.Dispose();

            base.Dispose(disposing);
        }

        private int ReadInternal(Span<byte> buffer)
        {
            int bytesToRead = Math.Min(buffer.Length, DataSectorSize - this.currentOffset);
            if (bytesToRead == 0)
            {
                if (this.currentSector >= this.sectors - 1)
                    return 0;

                this.currentSector++;
                this.currentOffset = 0;
            }

            this.baseStream.Position = (this.currentSector * RawSectorSize) + this.currentOffset + HeaderSize;
            int n = this.baseStream.Read(buffer[..bytesToRead]);
            this.currentOffset += n;
            if (this.currentOffset >= DataSectorSize)
            {
                this.currentSector++;
                this.currentOffset = 0;
            }

            return n;
        }
    }

    private sealed class AudioTrackPlayer : IDisposable
    {
        private const int SourceRate = 44100; // CD audio is always 44100Hz stereo 16-bit
        private const int SourceChannels = 2;
        private readonly Spice86.Audio.Backend.Audio.AudioPlayer audioPlayer;
        private readonly Stream audioStream;
        private readonly SemaphoreSlim syncLock = new(1, 1);
        private readonly Stopwatch playbackTimer = new();
        private readonly Spice86.Audio.Filters.Speex.SpeexResamplerCSharp? resampler;
        private readonly int outputRate;
        private CancellationTokenSource stopTokenSource = new();
        private Task? readTask;
        private int sectorsRead;
        private volatile bool playing;
        private bool disposed;

        public AudioTrackPlayer(string fileName)
        {
            this.audioStream = File.Open(fileName, new FileStreamOptions { Options = FileOptions.Asynchronous });
            var factory = new Spice86.Audio.Backend.Audio.AudioPlayerFactory(Spice86.Audio.Filters.AudioEngine.CrossPlatform);
            this.audioPlayer = factory.CreatePlayer(SourceRate, framesPerBuffer: 0, prebufferMs: 0, allowNegotiate: true);
            this.audioPlayer.Start();

            // The audio backend may negotiate a different output rate
            this.outputRate = this.audioPlayer.Format.SampleRate;
            if (this.outputRate != SourceRate)
            {
                // Use Speex resampler (same approach as DOSBox Staging's MixerChannel)
                // Quality 5 = SPEEX_RESAMPLER_QUALITY_DEFAULT (good balance of quality and speed)
                this.resampler = new Spice86.Audio.Filters.Speex.SpeexResamplerCSharp(
                    SourceChannels, (uint)SourceRate, (uint)this.outputRate, quality: 5);
            }
        }

        public bool Playing => this.playing;

        public long Position
        {
            get => this.sectorsRead * RawSectorSize;
            set
            {
                this.syncLock.Wait();
                try
                {
                    this.audioStream.Position = value;
                    this.sectorsRead = (int)(value % RawSectorSize);
                }
                finally
                {
                    this.syncLock.Release();
                }
            }
        }
        public long StopPosition { get; set; }

        public void Start()
        {
            if (this.playing)
                return;

            this.playing = true;
            this.playbackTimer.Start();
            this.readTask = Task.Run(this.ReadAndPlayAsync);
        }
        public void Stop()
        {
            if (!this.playing)
                return;

            this.playing = false;
            this.stopTokenSource.Cancel();
            this.readTask?.Wait();
            this.stopTokenSource.Dispose();
            this.stopTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Stop();
                this.audioPlayer.Dispose();
                this.audioStream.Dispose();
                this.stopTokenSource.Dispose();
                this.disposed = true;
            }
        }

        private async Task ReadAndPlayAsync()
        {
            try
            {
                var tempBuffer = new byte[2352]; // One CD sector: 2352 bytes = 588 stereo frames
                int sourceSamples = tempBuffer.Length / sizeof(short); // 1176 samples (588 stereo frames)
                var sourceFloats = new float[sourceSamples];

                // Output buffer: account for potential rate conversion
                double ratio = (double)this.outputRate / SourceRate;
                int maxOutputSamples = (int)(sourceSamples * ratio) + SourceChannels * 2;
                var outputFloats = new float[maxOutputSamples];

                while (this.playing)
                {
                    int bytesRead = 0;
                    while (bytesRead < tempBuffer.Length)
                    {
                        await this.syncLock.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            int n = await this.audioStream.ReadAsync(tempBuffer.AsMemory(bytesRead, tempBuffer.Length - bytesRead), this.stopTokenSource.Token).ConfigureAwait(false);
                            bytesRead += n;
                            if (n == 0)
                                break;
                        }
                        finally
                        {
                            this.syncLock.Release();
                        }
                    }

                    if (bytesRead > 0)
                    {
                        this.sectorsRead++;

                        // Convert 16-bit PCM to float
                        var shortSamples = MemoryMarshal.Cast<byte, short>(tempBuffer.AsSpan(0, bytesRead));
                        int sampleCount = shortSamples.Length;
                        for (int i = 0; i < sampleCount; i++)
                            sourceFloats[i] = shortSamples[i] / 32768f;

                        int writeCount;
                        float[] writeSource;

                        if (this.resampler != null)
                        {
                            // Use Speex resampler (matching DOSBox Staging's MixerChannel approach)
                            this.resampler.ProcessInterleavedFloat(
                                sourceFloats.AsSpan(0, sampleCount),
                                outputFloats.AsSpan(),
                                out uint inputConsumed,
                                out uint outputProduced);

                            writeCount = (int)outputProduced * SourceChannels;
                            writeSource = outputFloats;
                        }
                        else
                        {
                            // Source and output rates match — no resampling needed
                            writeCount = sampleCount;
                            writeSource = sourceFloats;
                        }

                        // Write using index-based tracking to avoid Span across await
                        int offset = 0;
                        while (offset < writeCount)
                        {
                            this.stopTokenSource.Token.ThrowIfCancellationRequested();
                            int written = this.audioPlayer.WriteData(writeSource.AsSpan(offset, writeCount - offset));
                            offset += written;
                            if (offset < writeCount)
                                await Task.Delay(1, this.stopTokenSource.Token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    [GeneratedRegex(@"^FILE\s+""(?<1>[^""]+)""\s+BINARY$", RegexOptions.ExplicitCapture)]
    private static partial Regex FileRegex();
    [GeneratedRegex(@"^\s+TRACK\s+(?<1>[0-9]+)\s+(?<2>.+)$", RegexOptions.ExplicitCapture)]
    private static partial Regex TrackRegex();
    [GeneratedRegex(@"^\s+INDEX\s+(?<1>[0-9]+)\s+(?<2>.+)$", RegexOptions.ExplicitCapture)]
    private static partial Regex IndexRegex();
    [GeneratedRegex(@"^\s+(?<1>PRE|POST)GAP\s+(?<2>.+)$", RegexOptions.ExplicitCapture)]
    private static partial Regex GapRegex();
}
