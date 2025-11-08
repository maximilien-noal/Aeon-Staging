using MeltySynth;
using Ownaudio.Core;

#nullable enable

namespace Aeon.Emulator.Sound
{
    internal sealed class MeltySynthMidiMapper : MidiDevice
    {
        private readonly Synthesizer synthesizer;
        private readonly IAudioEngine audioPlayer;
        private bool disposed;

        public MeltySynthMidiMapper(string soundFontPath)
        {
            if (string.IsNullOrEmpty(soundFontPath))
                throw new ArgumentNullException(nameof(soundFontPath));

            this.audioPlayer = Audio.CreatePlayer(true);
            // OwnAudioSharp uses 48kHz by default
            this.synthesizer = new Synthesizer(soundFontPath, 48000);
            // Engine is already started in CreatePlayer()
        }

        public override void Pause()
        {
            // Pause not implemented for now
        }
        public override void Resume()
        {
            // Resume not implemented for now
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
                if (!disposing)
                    this.audioPlayer.Dispose();

                this.disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
