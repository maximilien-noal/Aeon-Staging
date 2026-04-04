namespace Aeon.Emulator.Sound;

/// <summary>
/// Specifies the engine to use for MIDI playback.
/// </summary>
public enum MidiEngine
{
    /// <summary>
    /// Use the OS MIDI mapper. Windows-only (winmm.dll).
    /// On Linux and macOS, this falls back to silent (no MIDI output).
    /// For cross-platform MIDI, use <see cref="MeltySynth"/> or <see cref="Mt32"/> instead.
    /// </summary>
    MidiMapper,
    /// <summary>
    /// Use MeltySynth (requires soundfont).
    /// </summary>
    MeltySynth,
    /// <summary>
    /// Use munt (requires MT-32 roms).
    /// </summary>
    Mt32
}
