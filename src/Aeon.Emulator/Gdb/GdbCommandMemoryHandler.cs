using Aeon.Emulator.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aeon.Emulator.Gdb;

public class GdbCommandMemoryHandler {
    private readonly GdbFormatter _gdbFormatter = new();
    private readonly GdbIo _gdbIo;
    private readonly EmulatorHost _machine;

    public GdbCommandMemoryHandler(GdbIo gdbIo, EmulatorHost machine) {
        this._gdbIo = gdbIo;
        this._machine = machine;
    }

    public string ReadMemory(string commandContent) {
        try {
            String[] commandContentSplit = commandContent.Split(",");
            uint address = ConvertUtils.ParseHex32(commandContentSplit[0]);
            uint length = 1;
            if (commandContentSplit.Length > 1) {
                length = ConvertUtils.ParseHex32(commandContentSplit[1]);
            }

            System.Diagnostics.Debug.WriteLine($"Reading memory at address {address} for a length of {length}");
            var memory = _machine.VirtualMachine.PhysicalMemory;
            int memorySize = memory.MemorySize;
            var response = new StringBuilder((int)length * 2);
            for (long i = 0; i < length; i++) {
                long readAddress = address + i;
                if (readAddress >= memorySize) {
                    break;
                }

                byte b = memory.GetByte((uint)readAddress);
                string value = _gdbFormatter.FormatValueAsHex8(b);
                response.Append(value);
            }

            return _gdbIo.GenerateResponse(response.ToString());
        } catch (FormatException nfe) {
            System.Diagnostics.Debug.WriteLine($"Memory read requested but could not understand the request {commandContent}, {nfe.Message}");
            return _gdbIo.GenerateUnsupportedResponse();
        }
    }

    public string SearchMemory(string command) {
        String[] parameters = command.Replace("Search:memory:", "").Split(";");
        uint start = ConvertUtils.ParseHex32(parameters[0]);
        uint end = ConvertUtils.ParseHex32(parameters[1]);

        // read the bytes from the raw command as GDB does not send them as hex
        List<Byte> rawCommand = _gdbIo.RawCommand;

        // Extract the original hex sent by GDB, read from
        // 3: +$q
        // variable: header
        // 2: ;
        // variable 2 hex strings
        int patternStartIndex = 3 + "Search:memory:".Length + 2 + parameters[0].Length + parameters[1].Length;
        List<Byte> patternBytesList = rawCommand.GetRange(patternStartIndex, rawCommand.Count - 1);
        var memory = _machine.VirtualMachine.PhysicalMemory;
        uint? address = memory.SearchValue(start, (int)end, patternBytesList);
        if (address == null) {
            return _gdbIo.GenerateResponse("0");
        }

        return _gdbIo.GenerateResponse("1," + _gdbFormatter.FormatValueAsHex32(address.Value));
    }

    public string WriteMemory(string commandContent) {
        try {
            String[] commandContentSplit = commandContent.Split("[,:]");
            uint address = ConvertUtils.ParseHex32(commandContentSplit[0]);
            uint length = ConvertUtils.ParseHex32(commandContentSplit[1]);
            byte[] data = ConvertUtils.HexToByteArray(commandContentSplit[2]);
            if (length != data.Length) {
                return _gdbIo.GenerateResponse("E01");
            }

            var memory = _machine.VirtualMachine.PhysicalMemory;
            if (address + length > memory.MemorySize) {
                return _gdbIo.GenerateResponse("E02");
            }

            memory.WriteBytes(address, data);
            return _gdbIo.GenerateResponse("OK");
        } catch (FormatException nfe) {
            System.Diagnostics.Debug.WriteLine($"Memory write requested but could not understand the request {commandContent}");
            return _gdbIo.GenerateUnsupportedResponse();
        }
    }
}