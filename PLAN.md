# Aeon Cross-Platform Migration Plan

## Overview

This document outlines the plan to make Aeon run on **Windows, Linux, and macOS** by replacing Windows-only dependencies with cross-platform alternatives. The core emulation engine (`Aeon.Emulator`) already targets `net10.0` and has no platform-specific code — the work is concentrated in the **audio/sound** and **UI** layers.

---

## Current Platform-Specific Inventory

| Area | File(s) | Windows Dependency | Impact |
|------|---------|-------------------|--------|
| **Audio playback** | `Aeon.Emulator.Sound/Audio.cs` | `TinyAudio` NuGet (v0.5.0) | All sound output |
| **MIDI passthrough** | `Aeon.Emulator.Sound/Midi/WindowsMidiMapper.cs`, `NativeMethods.cs` | `winmm.dll` P/Invoke (`midiOutOpen`, `midiOutShortMsg`, etc.) | Windows MIDI Mapper device |
| **WPF UI** | `Aeon/` (10 XAML files, code-behind, converters, `FastBitmap.cs`, `WpfSynchronizer.cs`, `KeyExtensions.cs`) | WPF, `InteropBitmap`, `System.Windows.Input.Key`, `Dispatcher` | Entire desktop frontend |
| **Fast bitmap rendering** | `Aeon/FastBitmap.cs` | `kernel32.dll` P/Invoke (`CreateFileMapping`, `MapViewOfFile`) + WPF `InteropBitmap` | Video display |
| **Cursor control** | `Aeon/NativeMethods.cs` | `user32.dll` P/Invoke (`SetCursorPos`) | Mouse cursor repositioning |
| **CD-ROM device I/O** | `Aeon.DiskImages/Iso9660/NativeMethods.cs`, `IOCTL.cs` | `kernel32.dll` P/Invoke (`DeviceIoControl`, `CreateFile`, `ReadFile`) | Physical CD-ROM drive access |
| **Project targeting** | `Aeon/Aeon.csproj` | `net10.0-windows`, `UseWPF=true`, `UseWindowsForms=true` | Build configuration |

### Already cross-platform (no changes needed)

- `Aeon.Emulator` — Pure C# x86 emulation core, targets `net10.0`
- `Aeon.Emulator.Sound` (except MIDI passthrough and audio backend) — FM synth (`Ymf262Emu`), SoundFont MIDI (`MeltySynth`), MT-32 (`Mt32emu.net`), Sound Blaster, PC Speaker — all pure C#
- `Aeon.Emulator.Configuration`, `Aeon.DiskImages` (ISO file parsing), `AeonSourceGenerator`, `MooParser`
- `AeonMonoGame` — Already targets `net10.0` with MonoGame DesktopGL (cross-platform)

---

## Phase 1: Audio Backend — Replace TinyAudio with Bufdio.Spice86

### Rationale
`TinyAudio` is the current audio output abstraction. `Bufdio.Spice86` (the NuGet name for Spice86.Audio) is a cross-platform audio playback library built on **PortAudio**, supporting Windows, macOS, and Linux. Current latest version: **11.1.0**.

### Changes Required

1. **`Aeon.Emulator.Sound/Aeon.Emulator.Sound.csproj`**
   - Replace `<PackageReference Include="TinyAudio" Version="0.5.0" />` with `<PackageReference Include="Bufdio.Spice86" Version="11.1.0" />`

2. **`Aeon.Emulator.Sound/Audio.cs`**
   - Replace `using TinyAudio;` with the appropriate `Bufdio` namespace
   - Update `CreatePlayer()` to use `Bufdio.Spice86`'s audio player creation API
   - Update `WriteFullBuffer()` overloads to match the new player's `WriteData` method signature
   - The general pattern (write loop with `Thread.Sleep(1)` backoff) should remain the same

3. **All consumers of `Audio.CreatePlayer()`** — Verify that `InternalSpeaker.cs`, `SoundBlaster.cs` (via `Dsp.cs`), `FmSoundCard.cs`, and `MeltySynthMidiMapper.cs` still compile. These files call `Audio.CreatePlayer()` and `Audio.WriteFullBuffer()`, so if the `Audio` wrapper API stays the same, no changes are needed in consumers.

### Testing
- Verify Sound Blaster, OPL3 FM, PC Speaker, and MeltySynth MIDI all produce audio on Windows, Linux, and macOS

---

## Phase 2: General MIDI Passthrough — Windows-Only with Platform Guard

### Rationale
The Windows MIDI Mapper (`winmm.dll`) has no direct cross-platform equivalent. The existing code already handles this gracefully: `GeneralMidi.TryCreateMidiMapper()` returns `null` on non-Windows, which means MIDI simply doesn't play (silent). The `MeltySynth` and `MT-32` engines are fully cross-platform alternatives.

### Changes Required

1. **`Aeon.Emulator.Sound/Midi/WindowsMidiMapper.cs`** — Already has `[SupportedOSPlatform("windows")]`. **No changes needed.**

2. **`Aeon.Emulator.Sound/NativeMethods.cs`** — Already has `[SupportedOSPlatform("windows")]`. **No changes needed.**

3. **`Aeon.Emulator.Sound/Midi/GeneralMidi.cs`** (line 133) — The fallback already checks `OperatingSystem.IsWindows()`:
   ```csharp
   _ => OperatingSystem.IsWindows() ? new WindowsMidiMapper() : null
   ```
   - **Add a `// TODO: Implement MIDI passthrough for Linux and macOS` comment** on the `null` path to document the gap
   - Optionally, log a warning when on non-Windows and the user selects the `MidiMapper` engine, informing them that native MIDI passthrough is not available and suggesting MeltySynth or MT-32 as alternatives

4. **`Aeon.Emulator.Sound/Midi/MidiEngine.cs`** — Consider adding documentation that `MidiMapper` is Windows-only

### Non-Windows MIDI Strategy
- **Default recommendation**: Use `MeltySynth` with a SoundFont file (e.g., FluidR3_GM.sf2) for cross-platform General MIDI
- **MT-32**: Already cross-platform via `Mt32emu.net`
- **Future**: Investigate cross-platform MIDI passthrough via ALSA (Linux) or CoreMIDI (macOS) — but this is out of scope for the initial port

---

## Phase 3: UI — Replace WPF with AvaloniaUI

### Rationale
WPF is Windows-only. AvaloniaUI is a cross-platform XAML-based UI framework for .NET with an API very close to WPF. It uses Skia for GPU-accelerated rendering and runs on Windows, macOS, and Linux.

### Strategy
Create a new `Aeon.Avalonia` project that replaces the `Aeon` (WPF) project. The existing `Aeon` project can be kept for reference or removed. The `AeonMonoGame` project remains as an alternative frontend.

### Changes Required

#### 3.1 New Project Setup

1. **Create `src/Aeon.Avalonia/Aeon.Avalonia.csproj`**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <OutputType>WinExe</OutputType>
       <TargetFramework>net10.0</TargetFramework>
       <RootNamespace>Aeon.Emulator.Launcher</RootNamespace>
       <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Avalonia" Version="11.*" />
       <PackageReference Include="Avalonia.Desktop" Version="11.*" />
       <PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
     </ItemGroup>
     <ItemGroup>
       <ProjectReference Include="..\Aeon.Emulator.Configuration\Aeon.Emulator.Configuration.csproj" />
       <ProjectReference Include="..\Aeon.Emulator\Aeon.Emulator.csproj" />
       <ProjectReference Include="..\Aeon.DiskImages\Aeon.DiskImages.csproj" />
       <ProjectReference Include="..\Aeon.Emulator.Sound\Aeon.Emulator.Sound.csproj" />
     </ItemGroup>
   </Project>
   ```

2. **Add to `Aeon.slnx`** solution file

#### 3.2 File-by-File Migration Map

| WPF File | Avalonia Equivalent | Migration Notes |
|----------|-------------------|-----------------|
| `App.xaml` / `App.xaml.cs` | `App.axaml` / `App.axaml.cs` | Replace `Application` base class with Avalonia's. Use `AppBuilder` for initialization. |
| `MainWindow.xaml` / `.cs` | `MainWindow.axaml` / `.cs` | XAML is nearly identical. Replace `xmlns` to Avalonia namespaces. `FolderBrowserDialog` → Avalonia `StorageProvider.OpenFolderPickerAsync()`. |
| `EmulatorDisplay.xaml` / `.cs` | `EmulatorDisplay.axaml` / `.cs` | **Most complex migration.** Replace `ContentControl` DependencyProperties with Avalonia `StyledProperty`. Replace WPF routed events with Avalonia routed events. Replace mouse/keyboard input from `System.Windows.Input` to `Avalonia.Input`. |
| `EmulatorDisplayResources.xaml` | `EmulatorDisplayResources.axaml` | Update resource dictionary syntax (minimal changes). |
| `TaskDialog.xaml` / `.cs` | `TaskDialog.axaml` / `.cs` | Port custom dialog. Avalonia has no built-in TaskDialog, but the custom implementation should port easily. |
| `TaskDialogTemplates.xaml` | `TaskDialogTemplates.axaml` | Convert DataTemplates to Avalonia syntax. |
| `PaletteDialog.xaml` / `.cs` | `PaletteDialog.axaml` / `.cs` | Straightforward port — grids, colors, basic controls. |
| `PerformanceWindow.xaml` / `.cs` | `PerformanceWindow.axaml` / `.cs` | Simple data display window — direct port. |
| `NumericUpDown.xaml` / `.cs` | `NumericUpDown.axaml` / `.cs` | Avalonia has a built-in `NumericUpDown` control — use it instead of the custom one. |
| `RoundButtonResources.xaml` | `RoundButtonResources.axaml` | Port button style/template to Avalonia styling syntax. |
| `FastBitmap.cs` | `AvaloniaBitmap.cs` (new) | **Major rewrite.** Replace `InteropBitmap` + Win32 memory mapping with Avalonia's `WriteableBitmap`. Use `WriteableBitmap.Lock()` to get a pixel buffer pointer. This is fully cross-platform. |
| `WpfSynchronizer.cs` | `AvaloniaSynchronizer.cs` | Replace `Dispatcher.BeginInvoke` with `Avalonia.Threading.Dispatcher.UIThread.Post()`. |
| `KeyExtensions.cs` | `KeyExtensions.cs` | Replace `System.Windows.Input.Key` enum with `Avalonia.Input.Key` enum. The key names are very similar but not identical — update the dictionary mappings. |
| `MouseButtonExtensions.cs` | `MouseButtonExtensions.cs` | Replace WPF `MouseButton` with `Avalonia.Input.PointerPointProperties` or `Avalonia.Input.MouseButton`. |
| `MouseModeConverter.cs` | `MouseModeConverter.cs` | Replace `IValueConverter` (WPF) with Avalonia's `IValueConverter` — same interface, different namespace. |
| `SpeedConverter.cs` | `SpeedConverter.cs` | Same as above — namespace change only. |
| `SimpleCommand.cs` | `SimpleCommand.cs` | `ICommand` is in `System.Windows.Input` for WPF but `System.Windows.Input.ICommand` is actually in `System.ObjectModel` — likely no change needed or use `ReactiveCommand` from Avalonia's ReactiveUI integration. |
| `NativeMethods.cs` (`SetCursorPos`) | Platform abstraction | For cursor warping: use Avalonia's pointer APIs or conditionally P/Invoke per platform. Avalonia doesn't have a direct equivalent, so this may need a platform-specific helper with `#if` or runtime OS checks. |
| `BrowseInfo.cs` | Remove/replace | Replace with Avalonia `StorageProvider` API for file/folder browsing. |

#### 3.3 Key Avalonia Differences from WPF

| WPF Concept | Avalonia Equivalent |
|-------------|-------------------|
| `DependencyProperty` | `StyledProperty<T>` or `DirectProperty<T>` |
| `RoutedEvent` | `RoutedEvent<T>` (similar API) |
| `Dispatcher.BeginInvoke()` | `Dispatcher.UIThread.Post()` |
| `.xaml` extension | `.axaml` extension |
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` |
| `InteropBitmap` | `WriteableBitmap` |
| `System.Windows.Input.Key` | `Avalonia.Input.Key` |
| `System.Windows.Media.Imaging` | `Avalonia.Media.Imaging` |
| `ContentControl` | `ContentControl` (same name, different namespace) |
| `Window` | `Window` (same name, different namespace) |
| `FolderBrowserDialog` | `IStorageProvider.OpenFolderPickerAsync()` |
| `System.Windows.Threading.DispatcherTimer` | `Avalonia.Threading.DispatcherTimer` |

#### 3.4 Video Rendering (FastBitmap replacement)

The current `FastBitmap.cs` uses Win32 memory-mapped files + WPF `InteropBitmap` for zero-copy rendering. The Avalonia replacement:

```
// Conceptual approach (not actual code):
// 1. Create WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888)
// 2. On each frame: Lock() → get FramebufferPointer → copy emulator framebuffer → Unlock()
// 3. Display via Image control with Source = writeableBitmap
```

This is slightly different (Bgra8888 vs Bgr32) but handles the same use case. Performance should be comparable since `WriteableBitmap.Lock()` gives a direct pointer.

---

## Phase 4: CD-ROM Device I/O — Platform Abstraction

### Current State
`Aeon.DiskImages/Iso9660/NativeMethods.cs` uses Win32 `DeviceIoControl` for physical CD-ROM drive access. This is already guarded with `[SupportedOSPlatform("windows")]`.

### Changes Required

1. **Keep the existing Windows implementation** as-is
2. **Add platform abstractions** for physical CD-ROM access:
   - Linux: Read from `/dev/cdrom` or `/dev/sr0` using standard file I/O + `ioctl()` calls
   - macOS: Use IOKit or standard file I/O on `/dev/disk*`
3. **ISO file support already works cross-platform** — only physical drive access needs platform code
4. **This can be deferred** — most users mount ISO files rather than using physical drives

---

## Phase 5: Build & CI Updates

### Changes Required

1. **Update `Aeon.slnx`** to include `Aeon.Avalonia` project
2. **Update or create CI workflows** to build and test on:
   - `windows-latest`
   - `ubuntu-latest`
   - `macos-latest`
3. **Publish artifacts** for all three platforms:
   - `dotnet publish -r win-x64`
   - `dotnet publish -r linux-x64`
   - `dotnet publish -r osx-x64`
   - `dotnet publish -r osx-arm64`
4. **Package native dependencies** (SDL2 libraries, PortAudio libraries from Bufdio.Spice86)

---

## Implementation Order & Dependencies

```
Phase 1 (Audio)     ──→  Can be done independently, unblocks sound on all platforms
Phase 2 (MIDI)      ──→  Minimal work, just documentation/TODO notes
Phase 3 (UI)        ──→  Largest effort; depends on Phase 1 for audio during testing
Phase 4 (CD-ROM)    ──→  Can be deferred; low priority
Phase 5 (CI)        ──→  After Phase 3 is complete
```

### Recommended order: **Phase 1 → Phase 2 → Phase 3 → Phase 5 → Phase 4**

- Phase 1 & 2 are quick wins that make the sound library cross-platform
- Phase 3 is the largest effort and the critical path for having a working cross-platform app
- Phase 4 is optional / deferrable
- Phase 5 ties everything together

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Bufdio.Spice86 API incompatibility with TinyAudio | Low | The `Audio.cs` wrapper abstracts the API; only one file needs updating |
| Avalonia XAML differences cause rendering issues | Medium | Test extensively on all platforms; use Avalonia DevTools for debugging |
| Performance regression in video rendering | Low | Avalonia `WriteableBitmap` provides direct pointer access similar to `InteropBitmap` |
| `SetCursorPos` has no direct Avalonia equivalent | Medium | Use platform-specific code with runtime OS detection; or use Avalonia's pointer capture APIs |
| Physical CD-ROM access on Linux/macOS | Low | Defer; ISO file support is already cross-platform |

---

## Summary of Files Changed per Phase

### Phase 1 (2 files modified)
- `Aeon.Emulator.Sound/Aeon.Emulator.Sound.csproj` — swap TinyAudio → Bufdio.Spice86
- `Aeon.Emulator.Sound/Audio.cs` — update API calls

### Phase 2 (1-2 files modified)
- `Aeon.Emulator.Sound/Midi/GeneralMidi.cs` — add TODO comment + optional logging
- `Aeon.Emulator.Sound/Midi/MidiEngine.cs` — add documentation (optional)

### Phase 3 (~5-8 new files)
- New `Aeon.Input/` project or files in `Aeon.Emulator/`
- `GamepadDevice.cs`, `JoystickDevice.cs`, SDL2 bindings or NuGet reference

### Phase 4 (~20 new/modified files)
- New `Aeon.Avalonia/` project with all AXAML files and code-behind
- New `AvaloniaBitmap.cs` replacing `FastBitmap.cs`
- New `AvaloniaSynchronizer.cs` replacing `WpfSynchronizer.cs`

### Phase 5 (2-4 new files)
- Platform-specific CD-ROM abstractions (can be deferred)

### Phase 6 (1-2 new files)
- CI workflow files (`.github/workflows/`)
