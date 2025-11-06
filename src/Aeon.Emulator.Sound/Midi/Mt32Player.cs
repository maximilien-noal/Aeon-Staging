using System;
using System.IO;
using System.IO.Compression;
using Mt32emu;
using Ownaudio.Core;

namespace Aeon.Emulator.Sound
{
    internal sealed class Mt32Player : IDisposable
    {
        private readonly Mt32Context context = new();
        private readonly IAudioEngine audioPlayer = Audio.CreatePlayer();
        private bool disposed;

        public Mt32Player(string romsPath)
        {
            if (string.IsNullOrWhiteSpace(romsPath))
                throw new ArgumentNullException(nameof(romsPath));

            this.LoadRoms(romsPath);

            // OwnAudioSharp uses 48kHz by default
            var analogMode = Mt32GlobalState.GetBestAnalogOutputMode(48000);
            this.context.AnalogOutputMode = analogMode;
            this.context.SetSampleRate(48000);

            this.context.OpenSynth();
            // Engine is already started in CreatePlayer()
        }

        public void PlayShortMessage(uint message) => this.context.PlayMessage(message);
        public void PlaySysex(ReadOnlySpan<byte> data) => this.context.PlaySysex(data);
        public void Pause() { /* Engine pause not needed for now */ }
        public void Resume() { /* Engine resume not needed for now */ }
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.context.Dispose();
                this.audioPlayer.Dispose();
                this.disposed = true;
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
}
