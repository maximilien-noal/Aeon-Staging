namespace Aeon.Emulator.Gdb;

using Aeon.Emulator.Gdb.Breakpoint;
using System;

public class MachineBreakpoints : IDisposable {
    private readonly BreakPointHolder _cycleBreakPoints = new();

    private readonly BreakPointHolder _executionBreakPoints = new();
    private readonly EmulatorHost _machine;
    private readonly PhysicalMemory _memory;

    private BreakPoint? _machineStopBreakPoint;
    private bool disposedValue;

    public MachineBreakpoints(EmulatorHost machine) {
        _machine = machine;
        _memory = machine.VirtualMachine.PhysicalMemory;
    }

    public void CheckBreakPoint() {
        CheckBreakPoints();
        PauseHandler.WaitIfPaused();
    }

    public PauseHandler PauseHandler { get; } = new();

    public void OnMachineStop() {
        if (_machineStopBreakPoint is not null) {
            _machineStopBreakPoint.Trigger();
            PauseHandler.WaitIfPaused();
        }
    }

    public void ToggleBreakPoint(BreakPoint? breakPoint, bool on) {
        if (breakPoint is null) {
            return;
        }
        BreakPointType? breakPointType = breakPoint.BreakPointType;
        if (breakPointType == BreakPointType.EXECUTION) {
            _executionBreakPoints.ToggleBreakPoint(breakPoint, on);
        } else if (breakPointType == BreakPointType.CYCLES) {
            _cycleBreakPoints.ToggleBreakPoint(breakPoint, on);
        } else if (breakPointType == BreakPointType.MACHINE_STOP) {
            _machineStopBreakPoint = breakPoint;
        } else {
            _memory.ToggleBreakPoint(breakPoint, on);
        }
    }

    private void CheckBreakPoints() {
        if (!_executionBreakPoints.IsEmpty) {
            uint address = _machine.VirtualMachine.GetIpPhysicalAddress();
            _executionBreakPoints.TriggerMatchingBreakPoints(address);
        }

        if (!_cycleBreakPoints.IsEmpty) {
            long cycles = _machine.TotalInstructions;
            _cycleBreakPoints.TriggerMatchingBreakPoints(cycles);
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                PauseHandler.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}