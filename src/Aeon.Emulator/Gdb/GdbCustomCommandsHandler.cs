namespace Aeon.Emulator.Gdb;

using Aeon.Emulator.Gdb.Breakpoint;
using Aeon.Emulator.Utils;

using System;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Handles custom GDB commands triggered in command line via the monitor prefix.<br/>
/// Custom commands list can be seen with the monitor help command.
/// </summary>
public class GdbCustomCommandsHandler {
    private readonly GdbIo _gdbIo;
    private readonly EmulatorHost _machine;
    private readonly Action<BreakPoint> _onBreakpointReached;

    public GdbCustomCommandsHandler(GdbIo gdbIo, EmulatorHost machine, Action<BreakPoint> onBreakpointReached) {
        _gdbIo = gdbIo;
        _machine = machine;
        _onBreakpointReached = onBreakpointReached;
    }

    public virtual string HandleCustomCommands(string command) {
        string[] commandSplit = command.Split(",");
        if (commandSplit.Length != 2) {
            return _gdbIo.GenerateResponse("");
        }

        byte[] customHex = ConvertUtils.HexToByteArray(commandSplit[1]);
        string custom = Encoding.UTF8.GetString(customHex);
        string[] customSplit = custom.Split(" ");
        return ExecuteCustomCommand(customSplit);
    }

    private string BreakCycles(string[] args) {
        if (args.Length < 2) {
            return InvalidCommand("breakCycles can only work with one argument.");
        }

        string cyclesToWaitString = args[1];
        if (long.TryParse(cyclesToWaitString, out var cyclesToWait)) {
            long currentCycles = _machine.TotalInstructions;
            long cyclesBreak = currentCycles + cyclesToWait;
            var breakPoint = new AddressBreakPoint(BreakPointType.CYCLES, cyclesBreak, _onBreakpointReached, true);
            _machine.MachineBreakpoints.ToggleBreakPoint(breakPoint, true);
            System.Diagnostics.Debug.WriteLine($"Breakpoint added for cycles!\n{@breakPoint}");

            return _gdbIo.GenerateMessageToDisplayResponse(
                $"Breakpoint added for cycles. Current cycles is {currentCycles}. Will wait for {cyclesToWait}. Will stop at {cyclesBreak}");
        }
        return InvalidCommand($"breakCycles argument needs to be a number. You gave {cyclesToWaitString}");
    }

    private string BreakCsIp(string[] args) {
        if (args.Length < 3) {
            return InvalidCommand("breakCsIp can only work with two arguments.");
        }
        try {
            uint cs = ConvertUtils.ParseHex32(args[1]);
            uint ip = ConvertUtils.ParseHex32(args[2]);
            var breakPoint = new AddressBreakPoint(BreakPointType.EXECUTION, MemoryUtils.ToPhysicalAddress((ushort)cs, (ushort)ip), _onBreakpointReached, false);
            _machine.MachineBreakpoints.ToggleBreakPoint(breakPoint, true);
            System.Diagnostics.Debug.WriteLine($"Breakpoint added for cs:ip!\n{@breakPoint}");

            return _gdbIo.GenerateMessageToDisplayResponse(
                $"Breakpoint added for cs:ip. Current cs:ip is {_machine.VirtualMachine.Processor.CS}:{_machine.VirtualMachine.Processor.IP}. Will stop at {cs}:{ip}");
        } catch (FormatException fe) {
            return InvalidCommand($"breakCsIp arguments need to be two numbers. You gave {args[1]}:{args[2]}");
        }
    }

    private string BreakStop() {
        BreakPoint breakPoint = new UnconditionalBreakPoint(BreakPointType.MACHINE_STOP, _onBreakpointReached, false);
        _machine.MachineBreakpoints.ToggleBreakPoint(breakPoint, true);
        System.Diagnostics.Debug.WriteLine($"Breakpoint added for end of execution!\n{@breakPoint}");
 
        return _gdbIo.GenerateMessageToDisplayResponse("Breakpoint added for end of execution.");
    }

    private string CallStack() {
        return _gdbIo.GenerateUnsupportedResponse();
    }

    private string ExecuteCustomCommand(params string[] args) {
        string originalCommand = args[0];
        string command = originalCommand.ToLowerInvariant();
        return command switch {
            "help" => Help(""),
            "state" => State(),
            "breakstop" => BreakStop(),
            "callstack" => CallStack(),
            "peekret" => PeekRet(args),
            "breakcycles" => BreakCycles(args),
            "breakcsip" => BreakCsIp(args),
            _ => InvalidCommand(originalCommand),
        };
    }

    private string GetValidRetValues() {
        return string.Join(", ", Enum.GetNames(typeof(CallType)));
    }

    private string Help(string additionnalMessage) {
        return _gdbIo.GenerateMessageToDisplayResponse($@"{additionnalMessage}
Supported custom commands:
 -help: display this
 - breakCycles <number of cycles to wait before break>: breaks after the given number of cycles is reached
 - breakCsIp <number for CS, number for IP>: breaks once CS and IP match and before the instruction is executed
 - breakStop: setups a breakpoint when machine shuts down
 - callStack: dumps the callstack to see in which function you are in the VM.
 - peekRet<optional type>: displays the return address of the current function as stored in the stack in RAM. If a parameter is provided, dump the return on the stack as if the return was one of the provided type. Valid values are: {GetValidRetValues()}
 - state: displays the state of the machine
");
    }

    private string InvalidCommand(string command) {
        return Help($"Invalid command {command}\n");
    }

    private string PeekRet(string[] args) {
        if (args.Length == 1) {
            return _gdbIo.GenerateMessageToDisplayResponse(_machine.PeekReturn());
        } else {
            string returnType = args[1];
            bool parsed = Enum.TryParse(typeof(CallType), returnType, out object? callType);
            if (parsed == false) {
                return _gdbIo.GenerateMessageToDisplayResponse(
                    $"Could not understand {returnType} as a return type. Valid values are: {GetValidRetValues()}");
            }

            if (callType is CallType type) {
                return _gdbIo.GenerateMessageToDisplayResponse(_machine.PeekReturn(type));
            }
        }

        return "";
    }

    private string State() {
        var res = new StringBuilder();
        res.Append("Cycles").Append('=');
        res.Append(_machine.TotalInstructions);
        res.Append(" CS:IP=").Append(ConvertUtils.ToSegmentedAddressRepresentation(_machine.VirtualMachine.Processor.CS, _machine.VirtualMachine.Processor.IP)).Append('/').Append(ConvertUtils.ToHex(MemoryUtils.ToPhysicalAddress(_machine.VirtualMachine.Processor.CS, _machine.VirtualMachine.Processor.IP)));
        res.Append(" AX=").Append(ConvertUtils.ToHex16((ushort)_machine.VirtualMachine.Processor.AX));
        res.Append(" BX=").Append(ConvertUtils.ToHex16((ushort)_machine.VirtualMachine.Processor.BX));
        res.Append(" CX=").Append(ConvertUtils.ToHex16((ushort)_machine.VirtualMachine.Processor.CX));
        res.Append(" DX=").Append(ConvertUtils.ToHex16((ushort)_machine.VirtualMachine.Processor.DX));
        res.Append(" SI=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.SI));
        res.Append(" DI=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.DI));
        res.Append(" BP=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.BP));
        res.Append(" SP=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.SP));
        res.Append(" SS=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.SS));
        res.Append(" DS=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.DS));
        res.Append(" ES=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.ES));
        res.Append(" FS=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.FS));
        res.Append(" GS=").Append(ConvertUtils.ToHex16(_machine.VirtualMachine.Processor.GS));
        res.Append(" flags=").Append(ConvertUtils.ToHex16((ushort)_machine.VirtualMachine.Processor.Flags.Value));
        return _gdbIo.GenerateMessageToDisplayResponse(res.ToString());
    }
}