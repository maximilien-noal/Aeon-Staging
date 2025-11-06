# Aeon x86 Emulator - AI Coding Instructions

## Project Overview
Aeon is a high-performance x86 DOS emulator written in C# targeting .NET 9. It emulates a 486DX-era PC environment with aggressive JIT optimizations for performance-critical emulation loops.

## Architecture & Key Components

### Core Projects
- **`Aeon.Emulator`** - Core emulation engine with unsafe pointer-heavy processor implementation
- **`Aeon`** (main launcher) - WPF application with emulator UI and configuration system  
- **`Aeon.DiskImages`** - ISO/CUE disk image support and virtual file system
- **`Aeon.Emulator.Sound`** - Sound Blaster, OPL3 FM synthesis, MIDI support
- **`Aeon.Emulator.Input`** - DirectInput/XInput controller and keyboard/mouse handling
- **`AeonSourceGenerator`** - Source generator for x86 instruction implementations

### Critical Performance Considerations
**Always build in Release mode** - Debug builds are extremely slow due to suppressed JIT optimizations. The emulator relies heavily on:
- Aggressive inlining (`[MethodImpl(MethodImplOptions.AggressiveInlining)]`)
- Unsafe pointer operations for register access
- Hardware intrinsics (BMI1, etc.) in `Intrinsics.cs`
- Source-generated instruction dispatch tables

### Instruction Implementation System
Instructions are implemented in `src/Aeon.Emulator/Instructions/` using attributes:
```csharp
[Opcode("89/r rm32,r32", AddressSize = 16 | 32, OperandSize = 16 | 32)]
public static void MoveRegRm32(VirtualMachine vm, out uint dest, uint src)
```

The `AeonSourceGenerator` generates optimized dispatch code for different addressing/operand size combinations. All instruction methods follow specific patterns for operand loading/storing.

### Configuration System
Uses JSON `.AeonConfig` files for environment setup:
```json
{
  "startup-path": "C:\\",
  "drives": {
    "c": { "type": "fixed", "host-path": "D:\\DOS\\Games" }
  }
}
```

Drive types: `fixed`, `floppy35`, `floppy525`, `cdrom`. Supports host path mapping, ISO images, and archive files.

## Development Patterns

### Memory & Registers
- Processor state uses unsafe pointers: `processor.PAX`, `processor.PBX`, etc.
- Physical memory accessed via `PhysicalMemory.FetchInstruction()` and similar methods
- Register access through generated pointer arrays for performance

### Virtual Machine Structure
`VirtualMachine` orchestrates all components:
- `Processor` - CPU state and execution
- `PhysicalMemory` - Emulated RAM with video memory mapping
- `FileSystem` - DOS drive system with 26 virtual drives (A-Z)
- Device registration via `RegisterVirtualDevice()`

### Testing Approach
Tests in `Aeon.Test/` use helper methods like:
```csharp
vm.WriteCode("89 C3");  // mov ebx, eax (hex bytes)
vm.TestEmulator(10);    // Execute up to 10 instructions
```

### Debugging Features
- Built-in debugger with disassembly view (`DebuggerWindow`)
- Instruction logging with compressed binary format
- Performance monitoring window
- Register/memory viewers

## Common Pitfalls
- Never attach debugger during performance testing - kills JIT optimizations
- Instruction operand order follows Intel syntax (dest, src)
- ModRM byte handling is complex - see existing patterns in `Instructions/`
- Virtual drives require proper `DriveType` and `Mapping` configuration
- Source generator changes require full rebuild to take effect

## Key Files for Understanding
- `src/Aeon.Emulator/VirtualMachine.cs` - Main emulation orchestration
- `src/Aeon.Emulator/Decoding/InstructionSet.cs` - Core CPU emulation loop
- `src/Aeon.Emulator/Processor/Processor.cs` - Register state and unsafe operations
- `src/Aeon/Configuration/AeonConfiguration.cs` - JSON configuration schema
- `examples/*.AeonConfig` - Configuration file examples

## Build & Test Commands
```powershell
dotnet build src/Aeon.sln -c Release
dotnet test src/Aeon.Test/
```

Always use Release configuration for meaningful performance testing or emulation work.