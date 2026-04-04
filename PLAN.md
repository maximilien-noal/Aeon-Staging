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

## Phase 1: Audio Backend — Replace TinyAudio with Spice86.Audio

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

## Phase 3: UI — Replace WPF with AvaloniaUI (C# Code-Only, No XAML)

### Rationale
WPF is Windows-only. AvaloniaUI is a cross-platform UI framework for .NET with an API very close to WPF. It uses Skia for GPU-accelerated rendering and runs on Windows, macOS, and Linux.

### Approach: Pure C# — No AXAML/XAML Files
The Avalonia frontend will be written **entirely in C# code** (no `.axaml` files). This keeps the code as close as possible to the existing WPF code-behind style, where most logic is already in `.cs` files. The existing WPF project uses minimal XAML — the `EmulatorDisplay.xaml` is only 12 lines, `PaletteDialog.xaml` is 6 lines, and most UI logic lives in code-behind.

**Spice86** ([`OpenRakis/Spice86`](https://github.com/OpenRakis/Spice86)) is a good reference for Avalonia `WriteableBitmap` usage and rendering patterns, but its MVVM + XAML architecture should **not** be followed. Aeon's UI will stay close to the existing imperative, event-driven WPF code-behind pattern.

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

#### 3.2 File-by-File Migration Map (C#-Only)

All UI is built in **pure C# code** — no AXAML files. Controls are instantiated and composed programmatically.

| WPF File | Avalonia C# Equivalent | Migration Notes |
|----------|----------------------|-----------------|
| `App.xaml` / `App.xaml.cs` | `App.cs` (C# only) | Build `Application` subclass in C#. Use `AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime()`. Set theme via `Styles.Add(new FluentTheme())`. |
| `MainWindow.xaml` / `.cs` | `MainWindow.cs` (C# only) | Build the menu bar, toolbar, and layout in C# using `new Menu { Items = { ... } }`, `new StackPanel { Children = { ... } }`, etc. Most logic is already in code-behind — keep it there. Replace `FolderBrowserDialog` → `StorageProvider.OpenFolderPickerAsync()`. |
| `EmulatorDisplay.xaml` / `.cs` | `EmulatorDisplay.cs` (C# only) | The WPF XAML is only 12 lines (Viewbox → Canvas → Image). Build this trivially in C#: `new Viewbox { Child = new Canvas { Children = { displayImage } } }`. Replace `DependencyProperty` → `StyledProperty<T>`. Replace WPF `RoutedEvent` → Avalonia `RoutedEvent<T>`. Replace `System.Windows.Input` mouse/keyboard → `Avalonia.Input`. **Most complex file — all 380 lines of code-behind port directly.** |
| `EmulatorDisplayResources.xaml` | Inline in `EmulatorDisplay.cs` | The WPF resource dictionary defines styles with MultiTrigger + Storyboard animations. Convert to Avalonia pseudo-classes and `Transitions` in C# (e.g., `new Setter(OpacityProperty, 0.0)` with `DoubleTransition`). |
| `TaskDialog.xaml` / `.cs` | `TaskDialog.cs` (C# only) | Simple Grid + TextBlock + ItemsControl layout. Build in C# constructor. 2 `DependencyProperty` → `StyledProperty`. Event bubbling for button clicks ports directly. |
| `TaskDialogTemplates.xaml` | Inline in `TaskDialogItem.cs` | The WPF ControlTemplate (Grid + Rectangle + Image + TextBlocks) builds easily in C# as a `FuncControlTemplate<TaskDialogItem>`. |
| `PaletteDialog.xaml` / `.cs` | `PaletteDialog.cs` (C# only) | XAML is only 6 lines (Window + UniformGrid). Already creates 256 Rectangles programmatically in code-behind. Direct port — almost no changes. |
| `PerformanceWindow.xaml` / `.cs` | `PerformanceWindow.cs` (C# only) | Build layout in C# using `StackPanel`, `Expander`, `Label`. All updates are already imperative (`Label.Content = value`). |
| `NumericUpDown.xaml` / `.cs` | Use Avalonia's built-in `NumericUpDown` | Avalonia has a built-in `NumericUpDown` control. Drop the custom one entirely; bind `Value`, `Minimum`, `Maximum`, `Increment` properties. |
| `RoundButtonResources.xaml` | `RoundButton.cs` style helper | Port the Ellipse + gradient + animation template to C# using `new ControlTemplate<Button>` with Avalonia's animation API. Or simplify to a styled button. |
| `FastBitmap.cs` | `AvaloniaBitmap.cs` (C# only) | **Major rewrite.** Replace `InteropBitmap` + Win32 memory mapping with Avalonia's `WriteableBitmap`. Use `WriteableBitmap.Lock()` to get pixel buffer pointer. Reference Spice86's bitmap rendering for patterns. |
| `WpfSynchronizer.cs` | `AvaloniaSynchronizer.cs` | Replace `Dispatcher.BeginInvoke` with `Avalonia.Threading.Dispatcher.UIThread.Post()`. |
| `KeyExtensions.cs` | `KeyExtensions.cs` | Replace `System.Windows.Input.Key` → `Avalonia.Input.Key`. Update dictionary — key names are very similar. |
| `MouseButtonExtensions.cs` | `MouseButtonExtensions.cs` | Replace WPF `MouseButton` → `Avalonia.Input.PointerPointProperties`. Same switch logic. |
| `MouseModeConverter.cs` | `MouseModeConverter.cs` | `IValueConverter` is in `Avalonia.Data.Converters` — same interface pattern. Or replace with simple C# property logic (no converter needed in C#-only UI). |
| `SpeedConverter.cs` | `SpeedConverter.cs` or inline | Same — or replace with direct string formatting in code-behind. |
| `SimpleCommand.cs` | `SimpleCommand.cs` | `ICommand` is in `System.Windows.Input` namespace — works the same in Avalonia. No change needed. |
| `NativeMethods.cs` (`SetCursorPos`) | `CursorHelper.cs` (C# only) | Port SDL2 cursor warping logic to **pure C#** with per-platform P/Invoke to OS system libraries only: `user32.dll` (Windows), `libX11.so` (Linux Xorg), Wayland client libs (Linux Wayland), `CoreGraphics.framework` (macOS). No native SDL2 dependency. See section 3.5. |
| `BrowseInfo.cs` | Remove | Replace with Avalonia `StorageProvider` API. |
| `EmulationErrorRoutedEventArgs.cs` | `EmulationErrorRoutedEventArgs.cs` | Port to Avalonia `RoutedEventArgs` — same pattern, different base class. |
| `TaskDialogItem.cs` | `TaskDialogItem.cs` | Replace 3 `DependencyProperty` → `StyledProperty`. Same code structure. |

#### 3.3 Key Avalonia Differences from WPF (C#-Only Context)

| WPF Pattern | Avalonia C# Equivalent |
|-------------|----------------------|
| `DependencyProperty.Register(...)` | `StyledProperty<T>` via `AvaloniaProperty.Register<TOwner, T>(...)` |
| `DependencyProperty.RegisterReadOnly(...)` | `DirectProperty<TOwner, T>` via `AvaloniaProperty.RegisterDirect<TOwner, T>(...)` |
| `DependencyPropertyKey` (read-only) | `DirectProperty` with getter only |
| `RoutedEvent` + `RoutedEventHandler` | `RoutedEvent<RoutedEventArgs>` (similar registration pattern) |
| `Dispatcher.BeginInvoke()` | `Dispatcher.UIThread.Post()` |
| `new Window { Content = ... }` | Same — `new Window { Content = ... }` |
| `new Grid { RowDefinitions = ... }` | Same — `new Grid { RowDefinitions = ... }` |
| `element.SetBinding(...)` | `element.Bind(property, binding)` or `element[property] = new Binding(...)` |
| `ControlTemplate` in XAML | `new FuncControlTemplate<T>((control, scope) => ...)` in C# |
| `Style` with `Trigger` | `new Style(x => x.OfType<T>()) { Setters = { ... } }` + pseudo-classes |
| `DataTrigger` | Use `IObservable<T>` bindings or pseudo-classes in C# |
| `Storyboard` / `DoubleAnimation` | `new Animation { Duration = ..., Children = { new KeyFrame { Setters = { ... } } } }` |
| `InteropBitmap` | `WriteableBitmap` |
| `System.Windows.Input.Key` | `Avalonia.Input.Key` |
| `FolderBrowserDialog` (WinForms) | `IStorageProvider.OpenFolderPickerAsync()` |

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
4. **No native dependencies to package** — both Spice86.Audio and the cursor helper use only OS-provided system libraries via P/Invoke

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
| Spice86.Audio API incompatibility with TinyAudio | Low | The `Audio.cs` wrapper abstracts the API; only one file needs updating. Spice86.Audio is fully managed C# — no native dependencies to manage. |
| Avalonia XAML differences cause rendering issues | Medium | Test extensively on all platforms; use Avalonia DevTools for debugging |
| Performance regression in video rendering | Low | Avalonia `WriteableBitmap` provides direct pointer access similar to `InteropBitmap` |
| `SetCursorPos` replacement via pure C# SDL2 port | Low | Port uses only OS system libraries (`user32.dll`, `libX11.so`, `CoreGraphics.framework`); Wayland restrictions mitigated by porting SDL2's relative pointer protocol |
| Physical CD-ROM access on Linux/macOS | Low | Defer; ISO file support is already cross-platform |

---

## Summary of Files Changed per Phase

### Phase 1 (2 files modified)
- `Aeon.Emulator.Sound/Aeon.Emulator.Sound.csproj` — swap TinyAudio → Spice86.Audio
- `Aeon.Emulator.Sound/Audio.cs` — update API calls

### Phase 2 (1-2 files modified)
- `Aeon.Emulator.Sound/Midi/GeneralMidi.cs` — add TODO comment + optional logging
- `Aeon.Emulator.Sound/Midi/MidiEngine.cs` — add documentation (optional)

### Phase 3 (~20 new/modified files)
- New `Aeon.Avalonia/` project with all C#-only UI files (no AXAML)
- New `AvaloniaBitmap.cs` replacing `FastBitmap.cs`
- New `AvaloniaSynchronizer.cs` replacing `WpfSynchronizer.cs`
- New `CursorHelper.cs` — pure C# port of SDL2 cursor warping (per-platform P/Invoke to OS system libs only)

### Phase 4 (2-4 new files)
- Platform-specific CD-ROM abstractions (can be deferred)

### Phase 5 (1-2 new files)
- CI workflow files (`.github/workflows/`)
