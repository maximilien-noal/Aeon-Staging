# Aeon (Soft Fork)

Aeon is an x86 + DOS emulator written in C#.

This repository is a **soft fork** of the original [`gregdivis/Aeon`](https://github.com/gregdivis/Aeon), with a focus on:

- Keeping compatibility with upstream design and behavior where practical
- Improving maintainability and modern .NET support
- Expanding **cross-platform support**

## About the project

Aeon started in 2008 as a high-performance emulator experiment in C#/.NET. Conceptually, it is similar to [DOSBox](https://www.dosbox.com/), but with a different architecture and compatibility profile.

If you only want maximum DOS game compatibility, DOSBox is usually the better first choice.

## Downloads

- Original upstream releases: <https://github.com/gregdivis/Aeon/releases>
- For Linux/Mac, this fork can be considered as it uses Avaloniav11 and Spice86.Audio for full cross platform desktop support.

## Usage

The fastest way to start is using the **Quick Launch Program** button in the toolbar and selecting a DOS `.exe` or `.com` file.

You can also:

- Quick launch a command prompt in a directory to pass arguments before running programs
- Run batch files
- Launch a `.AeonConfig` JSON configuration file for a more detailed virtual environment setup

Sample configs are available in the upstream examples folder: <https://github.com/gregdivis/Aeon/tree/master/examples>

## Capabilities

Aeon targets the hardware/software environment of a typical early-1990s 486DX-era PC.

### CPU

- Core x86 instruction set
  - Nearly all instructions are implemented; some edge cases remain
- x87 FPU instructions
  - Emulated using 64-bit floating point math (not full 80-bit x87 precision)

### Memory

- Real mode memory model
- Protected mode memory model
  - Usable for many DOS apps with common DPMI extenders, though issues remain

### Video

- Text modes: `80x25`, `40x25`
- Graphics modes:
  - CGA (`320x200`, 4-color, mode `04h`)
  - EGA (`320x200`, `640x200`, `640x320`, 16-color, modes `0Dh`, `0Eh`, `10h`)
  - VGA (`640x480` 4-color, `320x200` 256-color, modes `12h`, `13h`)
  - Unchained VGA mode `13h` (Mode X)
  - SVGA VBE 2.0 (linear and windowed)
- Display filtering: `Scale2x`, `Scale3x`

### BIOS/System

- `8259` interrupt controller
- `8253/8254` programmable interval timer

### Peripherals

- PS/2 keyboard + interrupt handler
- PS/2 mouse + interrupt handler + mouse driver
- Game port (limited; currently DirectInput/XInput based)

### DOS layer

- Roughly equivalent to MS-DOS 5.0 behavior
- Command/batch interpreter
- Mountable drives:
  - Host directory
  - ISO image
  - BIN/CUE image
  - Host CD drive

### Sound

- Internal PC speaker (timer-based waveform generation only)
- OPL3/YMF262 FM synthesis (Sound Blaster/AdLib)
- Sound Blaster 16 DSP (primarily single/auto DMA mode)
- General MIDI via:
  - Windows MIDI Mapper
  - [MeltySynth](https://github.com/sinshu/meltysynth) (SoundFont-based)
  - [mt32emu](https://github.com/munt/munt) (requires MT-32 ROMs)

## Building

Build with Visual Studio (current supported version in this repo) or the `dotnet` CLI.

NuGet dependencies should restore automatically.

> **Important**
> Aeon is significantly slower in `Debug` builds, and can also slow down if a debugger is attached to `Release` builds. It relies heavily on inlining, intrinsics, and JIT optimizations that are reduced in debug scenarios.

## Upstream

- Original project: <https://github.com/gregdivis/Aeon>
