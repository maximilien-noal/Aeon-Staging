using Spice86.Audio.Backend.Audio;

namespace Aeon.Emulator.Sound.PCSpeaker;

/// <summary>
/// Emulates a PC speaker.
/// </summary>
public sealed class InternalSpeaker : IInputPort, IOutputPort, IDisposable
{
    /// <summary>
    /// Value into which the input frequency is divided to get the frequency in Hz.
    /// </summary>
    private const double FrequencyFactor = 1193180;
    private const float Amplitude = 0.5f;
    private const int OutputSampleRate = 44100;
    private const int BufferSize = 4410; // ~100ms at 44100Hz

    private volatile uint frequencyRegister;
    private byte? nextFrequencyRegisterByte;
    private volatile SpeakerControl controlRegister = SpeakerControl.UseTimer;
    private double phase;
    private AudioPlayer? audioPlayer;
    private Thread? playbackThread;
    private volatile bool playing;
    private volatile bool endPlayback;

    ReadOnlySpan<ushort> IInputPort.InputPorts => [0x61];
    ReadOnlySpan<ushort> IOutputPort.OutputPorts => [0x42, 0x61];

    byte IInputPort.ReadByte(int port) => (byte)this.controlRegister;
    ushort IInputPort.ReadWord(int port) => throw new NotImplementedException();
    void IOutputPort.WriteByte(int port, byte value)
    {
        if (port == 0x61)
        {
            var oldValue = this.controlRegister;
            this.controlRegister = (SpeakerControl)value;
            if (!oldValue.HasFlag(SpeakerControl.SpeakerOn) && this.controlRegister.HasFlag(SpeakerControl.SpeakerOn))
            {
                this.StartPlayback();
            }
            else if (oldValue.HasFlag(SpeakerControl.SpeakerOn) && !this.controlRegister.HasFlag(SpeakerControl.SpeakerOn))
            {
                this.StopPlayback();
            }
        }
        else
        {
            if (!this.nextFrequencyRegisterByte.HasValue)
            {
                this.nextFrequencyRegisterByte = value;
            }
            else
            {
                this.frequencyRegister = this.nextFrequencyRegisterByte.GetValueOrDefault() | ((uint)value << 8);
                this.nextFrequencyRegisterByte = null;
                this.phase = 0;
            }
        }
    }

    Task IVirtualDevice.PauseAsync()
    {
        this.StopPlayback();
        return Task.CompletedTask;
    }
    Task IVirtualDevice.ResumeAsync()
    {
        if (this.controlRegister.HasFlag(SpeakerControl.SpeakerOn))
            this.StartPlayback();
        return Task.CompletedTask;
    }
    void IDisposable.Dispose()
    {
        this.endPlayback = true;
        this.playing = false;
        this.playbackThread?.Join();
        this.audioPlayer?.Dispose();
        this.audioPlayer = null;
    }

    private void StartPlayback()
    {
        if (this.playing)
            return;

        this.audioPlayer ??= Audio.CreatePlayer(OutputSampleRate);
        this.playing = true;
        this.endPlayback = false;

        if (this.playbackThread is null || !this.playbackThread.IsAlive)
        {
            this.playbackThread = new Thread(this.PlaybackLoop) { IsBackground = true };
            this.playbackThread.Start();
        }
    }

    private void StopPlayback()
    {
        this.playing = false;
    }

    private void PlaybackLoop()
    {
        Span<float> buffer = stackalloc float[BufferSize];

        while (!this.endPlayback)
        {
            if (!this.playing)
            {
                Thread.Sleep(1);
                continue;
            }

            this.GenerateAudioData(buffer);
            Audio.WriteFullBuffer(this.audioPlayer!, buffer);
        }
    }

    private void GenerateAudioData(Span<float> buffer)
    {
        bool isOn = this.controlRegister.HasFlag(SpeakerControl.SpeakerOn);
        var frequency = FrequencyFactor / this.frequencyRegister;

        if (!isOn || frequency <= 0)
        {
            buffer.Clear();
            return;
        }

        var phaseIncrement = frequency / OutputSampleRate;

        for (int i = 0; i < buffer.Length; i++)
        {
            var normalizedPhase = this.phase % 1.0;
            buffer[i] = normalizedPhase < 0.5 ? Amplitude : -Amplitude;
            this.phase += phaseIncrement;
        }
    }
}
