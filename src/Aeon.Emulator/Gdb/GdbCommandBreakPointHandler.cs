using Aeon.Emulator.Gdb.Breakpoint;
using Aeon.Emulator.Utils;

using System;
using System.IO;

namespace Aeon.Emulator.Gdb;

public class GdbCommandBreakpointHandler {
    private readonly GdbIo _gdbIo;
    private readonly EmulatorHost _machine;
    private volatile bool _resumeEmulatorOnCommandEnd;

    public GdbCommandBreakpointHandler(GdbIo gdbIo, EmulatorHost machine) {
        _gdbIo = gdbIo;
        _machine = machine;
    }

    public string AddBreakpoint(string commandContent) {
        BreakPoint? breakPoint = ParseBreakPoint(commandContent);
        _machine.MachineBreakpoints.ToggleBreakPoint(breakPoint, true);
        System.Diagnostics.Debug.WriteLine($"Breakpoint added!\n{breakPoint}");
        
        return _gdbIo.GenerateResponse("OK");
    }

    public string ContinueCommand() {
        _resumeEmulatorOnCommandEnd = true;
        _machine.MachineBreakpoints.PauseHandler.RequestResume();

        // Do not send anything to GDB, CPU thread will send something when breakpoint is reached
        return _gdbIo.GenerateResponse("OK");
    }

    public bool ResumeEmulatorOnCommandEnd { get => _resumeEmulatorOnCommandEnd; set => _resumeEmulatorOnCommandEnd = value; }

    public void OnBreakPointReached(BreakPoint breakPoint) {
        System.Diagnostics.Debug.WriteLine($"Breakpoint reached!\n{breakPoint}");
        _machine.MachineBreakpoints.PauseHandler.RequestPause();
        _resumeEmulatorOnCommandEnd = false;
        try {
            _gdbIo.SendResponse(_gdbIo.GenerateResponse("S05"));
        } catch (IOException e) {
            System.Diagnostics.Debug.WriteLine($"IOException while sending breakpoint info: {e.Message}");
        }
    }

    public BreakPoint? ParseBreakPoint(String command) {
        try {
            string[] commandSplit = command.Split(",");
            int type = int.Parse(commandSplit[0]);
            long address = ConvertUtils.ParseHex32(commandSplit[1]);
            // 3rd parameter kind is unused in our case
            BreakPointType? breakPointType = type switch {
                0 => BreakPointType.EXECUTION,
                1 => BreakPointType.EXECUTION,
                2 => BreakPointType.WRITE,
                3 => BreakPointType.READ,
                4 => BreakPointType.ACCESS,
                _ => null
            };
            if (breakPointType == null) {
                System.Diagnostics.Debug.WriteLine($"Cannot parse breakpoint type {type} for command {command}");
                return null;
            }
            return new AddressBreakPoint((BreakPointType)breakPointType, address, this.OnBreakPointReached, false);
        } catch (FormatException nfe) {
            System.Diagnostics.Debug.WriteLine($"Cannot parse breakpoint {command}, {nfe.Message}");
            return null;
        }
    }

    public string RemoveBreakpoint(string commandContent) {
        BreakPoint? breakPoint = ParseBreakPoint(commandContent);
        if (breakPoint == null) {
            return _gdbIo.GenerateResponse("");
        }
        _machine.MachineBreakpoints.ToggleBreakPoint(breakPoint, false);
        System.Diagnostics.Debug.WriteLine($"Breakpoint removed!\n{breakPoint}");
        return _gdbIo.GenerateResponse("OK");
    }

    public string? Step() {
        _resumeEmulatorOnCommandEnd = true;

        // will pause the CPU at the next instruction unconditionally
        BreakPoint stepBreakPoint = new UnconditionalBreakPoint(BreakPointType.EXECUTION, this.OnBreakPointReached, true);
        _machine.MachineBreakpoints.ToggleBreakPoint(stepBreakPoint, true);
        System.Diagnostics.Debug.WriteLine($"Breakpoint added for step!\n{stepBreakPoint}");

        // Do not send anything to GDB, CPU thread will send something when breakpoint is reached
        return null;
    }
}