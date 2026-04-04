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
| **Cursor control** | `Aeon/NativeMethods.cs`, `EmulatorDisplay.xaml.cs` | `user32.dll` P/Invoke (`SetCursorPos`) | Mouse cursor warping for relative mouse mode — port SDL2 cursor code to pure C# for cross-platform support (Windows, Linux Xorg, Linux Wayland, macOS) using only OS system libraries |
| **CD-ROM device I/O** | `Aeon.DiskImages/Iso9660/NativeMethods.cs`, `IOCTL.cs` | `kernel32.dll` P/Invoke (`DeviceIoControl`, `CreateFile`, `ReadFile`) | Physical CD-ROM drive access |
| **Project targeting** | `Aeon/Aeon.csproj` | `net10.0-windows`, `UseWPF=true`, `UseWindowsForms=true` | Build configuration |

### Already cross-platform (no changes needed)

- `Aeon.Emulator` — Pure C# x86 emulation core, targets `net10.0`
- `Aeon.Emulator.Sound` (except MIDI passthrough and audio backend) — FM synth (`Ymf262Emu`), SoundFont MIDI (`MeltySynth`), MT-32 (`Mt32emu.net`), Sound Blaster, PC Speaker — all pure C#
- `Aeon.Emulator.Configuration`, `Aeon.DiskImages` (ISO file parsing), `AeonSourceGenerator`, `MooParser`
- `AeonMonoGame` — Already targets `net10.0` with MonoGame DesktopGL (cross-platform)

---

## Phase 1: Audio Backend — Replace TinyAudio with Spice86.Audio ✅ DONE

### Rationale
`TinyAudio` is the current audio output abstraction. **`Spice86.Audio`** (NuGet: `Spice86.Audio`, latest version: **11.4.0**) is a **fully managed, cross-platform** audio library with **no native dependencies**. It is a C# port combining code from multiple sources:

- **SDL2 cross-platform audio drivers** — Pure C# ports of SDL2's per-platform audio backends (WASAPI, ALSA, CoreAudio)
- **DOSBox Staging audio low-level code** — Audio structures, mixing, and processing ported from DOSBox Staging
- **IIR Filters** — Biquad filters (low-pass, high-pass, band-pass, notch, peaking, shelf)
- **Audio filters** — Chorus, Compressor, DC Block, Envelope, LFO, MVerb (reverb), Noise Gate, Crossfeed
- **Audio structures** — `AudioFrame` (stereo sample pair), `AudioFrameBuffer`, `RWQueue` (lock-free read/write queue)
- **Speex resampler** — High-quality audio resampling ported to C#

| Platform | Spice86.Audio Backend | Underlying OS API |
|----------|----------------------|-------------------|
| Windows | `SdlWindowsBackend` | WASAPI |
| Linux | `SdlLinuxBackend` | ALSA |
| macOS | `SdlMacBackend` | CoreAudio (AudioQueue) |

Source: [`OpenRakis/Spice86.Audio`](https://github.com/OpenRakis/Spice86.Audio)

### Key API Differences from TinyAudio

| TinyAudio (current) | Spice86.Audio (new) |
|---------------------|---------------------|
| `AudioPlayer.CreateDefault(TimeSpan, bool)` | `AudioPlayerFactory.CreatePlayer(sampleRate, framesPerBuffer, prebufferMs, allowNegotiate)` |
| `player.WriteData(ReadOnlySpan<float>)` → returns `int` | `player.WriteData(Span<float>)` → returns `int` |
| N/A | `player.Start()` — must be called to begin playback |
| N/A | `player.MuteOutput()` / `player.UnmuteOutput()` |
| N/A | `player.ClearQueuedData()` |

### Changes Required

1. **`Aeon.Emulator.Sound/Aeon.Emulator.Sound.csproj`**
   - Replace `<PackageReference Include="TinyAudio" Version="0.5.0" />` with `<PackageReference Include="Spice86.Audio" Version="11.4.0" />`

2. **`Aeon.Emulator.Sound/Audio.cs`**
   - Replace `using TinyAudio;` with `using Spice86.Audio.Backend.Audio;`
   - Replace `CreatePlayer()` — instantiate `AudioPlayerFactory` with `AudioEngine.CrossPlatform`, then call `factory.CreatePlayer(44100, 0, 0, true)` (or appropriate parameters)
   - Add `player.Start()` call after creation (Spice86.Audio requires explicit start)
   - Update `WriteFullBuffer()` — signature changes from `ReadOnlySpan<float>` to `Span<float>`; the `short` and `byte` overloads will need conversion to float before writing (Spice86.Audio only accepts `Span<float>`)
   - The write loop with `Thread.Sleep(1)` backoff pattern remains the same

3. **All consumers of `Audio.CreatePlayer()`** — Verify that `InternalSpeaker.cs`, `SoundBlaster.cs` (via `Dsp.cs`), `FmSoundCard.cs`, and `MeltySynthMidiMapper.cs` still compile. These files call `Audio.CreatePlayer()` and `Audio.WriteFullBuffer()`, so if the `Audio` wrapper API stays the same, no changes are needed in consumers.

### Testing
- Verify Sound Blaster, OPL3 FM, PC Speaker, and MeltySynth MIDI all produce audio on Windows, Linux, and macOS

### Implementation Notes

**Callback → push-based conversion**: TinyAudio had a callback mode where the OS audio system pulled data. Spice86.Audio only supports push mode (`player.WriteData(Span<float>)`). All callback-based consumers (InternalSpeaker, FmSoundCard, Mt32Player, MeltySynthMidiMapper) were converted to background thread push loops. SoundBlaster was already push-based; its `BeginPlayback()`/`StopPlayback()` calls were replaced with `MuteOutput()`/`UnmuteOutput()`.

**CD audio (CueSheetImage.cs)**: The CD audio player uses `SpeexResamplerCSharp` from Spice86.Audio for high-quality resampling when the audio backend negotiates a different output rate. This matches **DOSBox Staging's** [`cdrom_image.cpp`](https://github.com/dosbox-staging/dosbox-staging/blob/main/src/dos/cdrom_image.cpp) approach: CD audio is decoded from BIN/CUE image files, resampled via the mixer's Speex resampler, and fed to the audio output through a `MixerChannel`. DOSBox Staging also supports compressed audio tracks via SDL_sound (FLAC, MP3, OGG, OPUS, WAV). This is not yet implemented in Aeon but could be added later by decoding compressed tracks to PCM before feeding them to the existing resampling pipeline.

---

## Phase 2: General MIDI Passthrough — Windows-Only with Platform Guard ✅ DONE

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

## Phase 3: UI — Replace WPF with AvaloniaUI (AXAML + Code-Behind) ✅ DONE

### Rationale
WPF is Windows-only. AvaloniaUI is a cross-platform UI framework for .NET with an API very close to WPF. It uses Skia for GPU-accelerated rendering and runs on Windows, macOS, and Linux.

### Approach: AXAML + Code-Behind (Faithful WPF Port)
The Avalonia frontend uses **AXAML files** (`.axaml`) to faithfully port the WPF XAML layouts, styles, and templates. Code-behind (`.axaml.cs` / `.cs`) stays close to the existing WPF code-behind style. WPF `DependencyProperty` is replaced by Avalonia `StyledProperty<T>`, enabling Avalonia style selectors for visual states (opacity on pause, cursor hiding, etc.).

Resource images (toolbar icons, task dialog arrow) are copied from the WPF project and referenced via `avares://` URIs. The application icon (`Aeon.ico`) is included.

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
       <ApplicationIcon>Aeon.ico</ApplicationIcon>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Avalonia" Version="11.3.13" />
       <PackageReference Include="Avalonia.Desktop" Version="11.3.13" />
       <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.13" />
     </ItemGroup>
     <ItemGroup>
       <ProjectReference Include="..\Aeon.Emulator.Configuration\Aeon.Emulator.Configuration.csproj" />
       <ProjectReference Include="..\Aeon.Emulator\Aeon.Emulator.csproj" />
       <ProjectReference Include="..\Aeon.DiskImages\Aeon.DiskImages.csproj" />
       <ProjectReference Include="..\Aeon.Emulator.Sound\Aeon.Emulator.Sound.csproj" />
     </ItemGroup>
     <ItemGroup>
       <AvaloniaResource Include="Resources\*.png" />
       <AvaloniaResource Include="Aeon.ico" />
     </ItemGroup>
   </Project>
   ```

2. **Add to `Aeon.slnx`** solution file

#### 3.2 File-by-File Migration Map (AXAML + Code-Behind)

UI is defined in **AXAML files** (`.axaml`) faithfully porting WPF XAML, with **code-behind** (`.cs`) staying close to the WPF code-behind style. WPF `DependencyProperty` → Avalonia `StyledProperty<T>`.

| WPF File | Avalonia Equivalent | Migration Notes |
|----------|----------------------|-----------------|
| `App.xaml` / `App.xaml.cs` | `App.axaml` / `App.cs` | AXAML defines FluentTheme, StyleIncludes for EmulatorDisplayStyles + TaskDialogStyles, and `backgroundGradient` resource. Code-behind uses `AvaloniaXamlLoader.Load(this)`. |
| `MainWindow.xaml` / `.cs` | `MainWindow.axaml` / `MainWindow.cs` | AXAML defines full menu (_Aeon/_Edit/_View/_Debug), gradient toolbar with PNG icon buttons (play/pause polygon shapes, open folder, mouse integration), speed controls. Toolbar visibility bound to CheckBox via `{Binding #toolBarCheckBox.IsChecked}`. Code-behind handles file/folder dialogs via `StorageProvider`. |
| `EmulatorDisplay.xaml` / `.cs` | `EmulatorDisplay.axaml` / `EmulatorDisplay.cs` | AXAML ports the Viewbox → Canvas → Image layout. Code-behind uses `StyledProperty<T>` for all DPs (EmulatorState, MouseInputMode, IsMouseCursorCaptured, EmulationSpeed, IsAspectRatioLocked, ScalingAlgorithm) enabling style selectors. Static constructor registers property change handlers. |
| `EmulatorDisplayResources.xaml` | `EmulatorDisplayStyles.axaml` | Style selectors: `EmulatorDisplay[EmulatorState=Paused]` → Opacity 0.5, `[EmulatorState=Running][MouseInputMode=Absolute]` → Cursor None, `[EmulatorState=Running][IsMouseCursorCaptured=True]` → Cursor None, `[EmulatorState=ProgramExited]` → 2-second fade to Opacity 0.5 (Animation matching WPF Storyboard). |
| `TaskDialog.xaml` / `.cs` | `TaskDialog.axaml` / `TaskDialog.cs` | AXAML ports Grid + TextBlock + ItemsControl layout. Code-behind sets caption and items, handles button click → Close(true). |
| `TaskDialogTemplates.xaml` | `TaskDialogStyles.axaml` | AXAML ControlTemplate for TaskDialogItem: Border + Grid with TaskArrow.png icon, Text + Description via `TemplateBinding`. `:pointerover` style selector adds blue border/gradient background on hover. |
| `PaletteDialog.xaml` / `.cs` | `PaletteDialog.axaml` / `PaletteDialog.cs` | AXAML: Window + `UniformGrid Rows="16" Columns="16"`. Code-behind adds 256 Rectangles and updates colors at 30fps via DispatcherTimer. |
| `PerformanceWindow.xaml` / `.cs` | `PerformanceWindow.axaml` / `PerformanceWindow.cs` | AXAML ports DockPanel + ScrollViewer + Expanders (Processor, Memory) with gradient separator rectangles. Code-behind updates labels at 1-second intervals. |
| `NumericUpDown.xaml` / `.cs` | `NumericUpDown.axaml` / `NumericUpDown.cs` | AXAML ports Grid with TextBox + up/down Polygon buttons. Code-behind uses `StyledProperty<T>` with coerce callback for Value/Min/Max/Step/IsReadOnly. |
| `RoundButtonResources.xaml` | (Simplified into toolbar buttons) | Avalonia Fluent theme buttons used instead; toolbar uses gradient Border background and Polygon/Rectangle shapes for icons. |
| `FastBitmap.cs` | `AvaloniaBitmap.cs` | Replaces `InteropBitmap` + Win32 memory mapping with Avalonia's `WriteableBitmap(Bgra8888)`. Lock() returns pixel buffer Span<uint>. |
| `WpfSynchronizer.cs` | `AvaloniaSynchronizer.cs` | `Dispatcher.UIThread.Post()` replaces `Dispatcher.BeginInvoke`. |
| `KeyExtensions.cs` | `KeyExtensions.cs` | `Avalonia.Input.Key` → emulator `Keys` mapping. FrozenDictionary lookup. |
| `MouseButtonExtensions.cs` | `MouseButtonExtensions.cs` | `Avalonia.Input.PointerUpdateKind` → emulator `MouseButtons`. |
| `MouseModeConverter.cs` | (Replaced by direct event handler) | `AspectRatioCheckBox_Changed` handler in MainWindow code-behind. |
| `SpeedConverter.cs` | (Replaced by `FormatSpeed()` method) | Inline `FormatSpeed()` in MainWindow code-behind. |
| `SimpleCommand.cs` | `SimpleCommand.cs` | `ICommand` (System.Windows.Input) — unchanged, works in Avalonia. |
| `NativeMethods.cs` | `CursorHelper.cs` | Cross-platform cursor warping: `user32.dll` (Windows), `libX11.so` (Linux Xorg), `CoreGraphics.framework` (macOS). |
| `BrowseInfo.cs` | (Removed) | Replaced by Avalonia `StorageProvider` API. |
| `EmulationErrorRoutedEventArgs.cs` | `EmulationErrorRoutedEventArgs.cs` | Avalonia `RoutedEventArgs` subclass. |
| `TaskDialogItem.cs` | `TaskDialogItem.cs` | `StyledProperty<T>` for Text/Description, enabling `TemplateBinding` in AXAML ControlTemplate. |
| `Resources/*.png` | `Resources/*.png` | Toolbar icons (openfolderHS.png, MouseIntegration.png) and TaskArrow.png copied to Avalonia project. Referenced via `avares://Aeon.Avalonia/Resources/` URIs. |
| `Aeon.ico` | `Aeon.ico` | Application icon copied to Avalonia project, referenced in csproj `<ApplicationIcon>`. |

#### 3.3 Key Avalonia Differences from WPF

| WPF Pattern | Avalonia Equivalent |
|-------------|----------------------|
| `DependencyProperty.Register(...)` | `StyledProperty<T>` via `AvaloniaProperty.Register<TOwner, T>(...)` |
| `DependencyProperty.RegisterReadOnly(...)` | `StyledProperty<T>` with private setter (for style selector support) |
| `RoutedEvent` + `RoutedEventHandler` | `RoutedEvent<RoutedEventArgs>` (similar registration pattern) |
| `Dispatcher.BeginInvoke()` | `Dispatcher.UIThread.Post()` |
| XAML `Style.Triggers` / `MultiTrigger` | AXAML `Style Selector="Type[Property=Value]"` |
| XAML `ControlTemplate.Triggers` | AXAML pseudo-class selectors (`:pointerover`, etc.) |
| `Storyboard` / `DoubleAnimation` | Avalonia `Animation` with `KeyFrame` for animated transitions (e.g., 2-second fade) |
| `InteropBitmap` | `WriteableBitmap` (Bgra8888) |
| `System.Windows.Input.Key` | `Avalonia.Input.Key` |
| `FolderBrowserDialog` (WinForms) | `IStorageProvider.OpenFolderPickerAsync()` |
| `OpenFileDialog` (WPF/WinForms) | `IStorageProvider.OpenFilePickerAsync()` |
| `Clipboard.SetImage(bmp)` | `DataObject` with PNG bytes via `SetDataObjectAsync` |
| Image Source path `"Resources/file.png"` | `avares://AssemblyName/Resources/file.png"` URI |
| `Visibility.Collapsed` / `Visible` | `IsVisible = false` / `true` |
| `ToolTip="text"` | `ToolTip.Tip="text"` |
| `UniformGrid` | `UniformGrid` (same API) |

#### 3.4 Video Rendering (FastBitmap replacement)

The current `FastBitmap.cs` uses Win32 memory-mapped files + WPF `InteropBitmap` for zero-copy rendering. The Avalonia replacement:

```
// Conceptual approach (not actual code):
// 1. Create WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888)
// 2. On each frame: Lock() → get FramebufferPointer → copy emulator framebuffer → Unlock()
// 3. Display via Image control with Source = writeableBitmap
```

This is slightly different (Bgra8888 vs Bgr32) but handles the same use case. Performance should be comparable since `WriteableBitmap.Lock()` gives a direct pointer.

#### 3.5 Cross-Platform Cursor Control — Pure C# Port of SDL2 Cursor Code

The current `NativeMethods.SetCursorPos()` (Win32 `user32.dll`) is used in `EmulatorDisplay.xaml.cs` (line 375) to warp the mouse cursor back to the center of the display during relative mouse capture mode. This is essential for FPS-style mouse look in DOS games.

**Avalonia does not provide a built-in cursor warp API**, so a cross-platform solution is needed. Following the same approach as Spice86.Audio (which ports SDL2 audio drivers to pure C#), the SDL2 cursor warping code will be **ported to pure C#** — no native SDL2 library dependency.

##### Per-Platform C# Ports

| Platform | SDL2 Source Reference | C# Port Approach |
|----------|----------------------|-----------------|
| Windows | `SDL_windowsmouse.c` → `WIN_WarpMouse()` | P/Invoke `user32.dll SetCursorPos()` (same as current, but isolated in cross-platform abstraction) |
| Linux (Xorg) | `SDL_x11mouse.c` → `X11_WarpMouse()` | P/Invoke `libX11.so` → `XWarpPointer()` |
| Linux (Wayland) | `SDL_waylandmouse.c` → `Wayland_WarpMouse()` | P/Invoke Wayland client libs for `wl_pointer` warp (with compositor support) |
| macOS | `SDL_cocoamouse.m` → `Cocoa_WarpMouse()` | P/Invoke `CoreGraphics.framework` → `CGWarpMouseCursorPosition()` |

##### Changes Required

1. **Create `CursorHelper.cs`** in `Aeon.Avalonia` (or a shared project):
   - Port the SDL2 cursor warping logic to C# with per-platform P/Invoke
   - Use `OperatingSystem.IsWindows()` / `IsLinux()` / `IsMacOS()` for runtime platform detection
   - Each platform path uses only OS-provided system libraries (no SDL2 native dependency)
   - Single public API: `CursorHelper.WarpCursor(int x, int y)`

2. **Update `EmulatorDisplay` code-behind** — replace the `NativeMethods.SetCursorPos()` call with `CursorHelper.WarpCursor()`

3. **No native library shipping required** — all P/Invoke targets are OS-provided system libraries (`user32.dll`, `libX11.so`, `CoreGraphics.framework`)

##### Wayland Note
Wayland has restrictions on cursor warping for security reasons — some compositors may not honor warp requests. For relative mouse input on Wayland, SDL2's approach uses relative pointer protocol (`zwp_relative_pointer_v1`) to capture mouse motion deltas directly without warping. This may need to be ported as well for full Wayland support.

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

## Phase 5: Build & CI Updates ✅ DONE

### Changes Required

1. ~~**Update `Aeon.slnx`** to include `Aeon.Avalonia` project~~ ✅ Already done in Phase 3
2. ~~**Update or create CI workflows** to build and test on:~~ ✅
   - `windows-latest` — builds full solution (including WPF project)
   - `ubuntu-latest` — builds cross-platform projects only
   - `macos-latest` — builds cross-platform projects only
3. ~~**Publish artifacts** for all three platforms:~~ ✅
   - `dotnet publish -r win-x64`
   - `dotnet publish -r linux-x64`
   - `dotnet publish -r osx-arm64`
4. **No native dependencies to package** — both Spice86.Audio and the cursor helper use only OS-provided system libraries via P/Invoke

---

## Implementation Order & Dependencies

```
Phase 1 (Audio)     ──→  Can be done independently, unblocks sound on all platforms
Phase 2 (MIDI)      ──→  Minimal work, just documentation/TODO notes
Phase 3 (UI)        ──→  Largest effort; depends on Phase 1 for audio during testing
Phase 4 (CD-ROM)    ──→  Can be deferred; low priority
Phase 5 (CI)        ──→  ✅ Done — multi-platform build matrix
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
| Spice86.Audio API incompatibility with TinyAudio | Low | The `Audio.cs` wrapper abstracts the API; only one file needs updating. Spice86.Audio is fully managed C# — no native dependencies to manage. |
| Avalonia XAML differences cause rendering issues | Medium | Test extensively on all platforms; use Avalonia DevTools for debugging |
| Performance regression in video rendering | Low | Avalonia `WriteableBitmap` provides direct pointer access similar to `InteropBitmap` |
| `SetCursorPos` replacement via pure C# SDL2 port | Low | Port uses only OS system libraries (`user32.dll`, `libX11.so`, `CoreGraphics.framework`); Wayland restrictions mitigated by porting SDL2's relative pointer protocol |
| Physical CD-ROM access on Linux/macOS | Low | Defer; ISO file support is already cross-platform |

---

## Summary of Files Changed per Phase

### Phase 1 ✅ (9 files modified)
- `Aeon.Emulator.Sound/Aeon.Emulator.Sound.csproj` — swap TinyAudio → Spice86.Audio 11.4.0
- `Aeon.Emulator.Sound/Audio.cs` — rewrite using `AudioPlayerFactory` + `AudioEngine.CrossPlatform`; add float conversion for `short[]`/`byte[]` overloads
- `Aeon.Emulator.Sound/PCSpeaker/InternalSpeaker.cs` — callback → push-based background thread
- `Aeon.Emulator.Sound/FM/FmSoundCard.cs` — callback → push-based background thread
- `Aeon.Emulator.Sound/Midi/Mt32Player.cs` — callback → push-based background thread
- `Aeon.Emulator.Sound/Midi/MeltySynthMidiMapper.cs` — callback → push-based background thread
- `Aeon.Emulator.Sound/Blaster/SoundBlaster.cs` — replace `BeginPlayback()`/`StopPlayback()` with `MuteOutput()`/`UnmuteOutput()`
- `Aeon.DiskImages/Aeon.DiskImages.csproj` — swap TinyAudio → Spice86.Audio 11.4.0
- `Aeon.DiskImages/CueSheetImage.cs` — replace TinyAudio AudioPlayer with Spice86.Audio; use `SpeexResamplerCSharp` (DOSBox Staging MixerChannel approach) for CD audio resampling

### Phase 2 ✅ (2 files modified)
- `Aeon.Emulator.Sound/Midi/GeneralMidi.cs` — add TODO comment for Linux/macOS MIDI passthrough
- `Aeon.Emulator.Sound/Midi/MidiEngine.cs` — document MidiMapper as Windows-only

### Phase 3 ✅ (9 new AXAML files, 3 PNG resources, 1 icon, 1 new control, 7 C# files rewritten, 1 modified)
- New `Aeon.Avalonia/App.axaml` — FluentTheme, StyleIncludes, backgroundGradient resource
- Rewritten `Aeon.Avalonia/App.cs` — code-behind with AvaloniaXamlLoader.Load()
- New `Aeon.Avalonia/MainWindow.axaml` — Full menu, gradient toolbar with PNG icon buttons, speed controls
- Rewritten `Aeon.Avalonia/MainWindow.cs` — code-behind with file dialogs via StorageProvider
- New `Aeon.Avalonia/EmulatorDisplay.axaml` — Viewbox/Canvas/Image layout (faithful WPF port)
- Rewritten `Aeon.Avalonia/EmulatorDisplay.cs` — StyledProperty for all DPs, static change handlers
- New `Aeon.Avalonia/EmulatorDisplayStyles.axaml` — Style selectors for Paused/Running/ProgramExited states
- New `Aeon.Avalonia/TaskDialog.axaml` — Grid + TextBlock + ItemsControl layout
- Rewritten `Aeon.Avalonia/TaskDialog.cs` — code-behind with ItemsSource binding
- New `Aeon.Avalonia/TaskDialogStyles.axaml` — ControlTemplate with icon + hover animation
- Rewritten `Aeon.Avalonia/TaskDialogItem.cs` — StyledProperty for Text/Description (TemplateBinding support)
- New `Aeon.Avalonia/PaletteDialog.axaml` — UniformGrid 16×16 (replaces WrapPanel)
- Rewritten `Aeon.Avalonia/PaletteDialog.cs` — code-behind with DispatcherTimer color updates
- New `Aeon.Avalonia/PerformanceWindow.axaml` — Expanders with gradient separators (faithful WPF port)
- Rewritten `Aeon.Avalonia/PerformanceWindow.cs` — code-behind with timer updates
- New `Aeon.Avalonia/NumericUpDown.axaml` — TextBox + up/down Polygon buttons
- New `Aeon.Avalonia/NumericUpDown.cs` — StyledProperty with coerce callback
- New `Aeon.Avalonia/Resources/openfolderHS.png`, `MouseIntegration.png`, `TaskArrow.png` — toolbar icons
- New `Aeon.Avalonia/Aeon.ico` — application icon
- Modified `Aeon.Avalonia/Aeon.Avalonia.csproj` — ApplicationIcon, AvaloniaResource includes
- Modified `Aeon.slnx` — added Aeon.Avalonia project

### Phase 4 (2-4 new files)
- Platform-specific CD-ROM abstractions (can be deferred)

### Phase 5 ✅ (1 new file)
- `.github/workflows/build.yml` — multi-platform CI workflow (Windows/Linux/macOS), builds + tests + publishes artifacts
