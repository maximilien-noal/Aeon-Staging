using Aeon.Emulator.Utils;

using System;
using System.Text;

namespace Aeon.Emulator.Gdb;

public class GdbCommandRegisterHandler {
    private readonly GdbFormatter _gdbFormatter = new();
    private readonly GdbIo _gdbIo;
    private readonly EmulatorHost _machine;

    public GdbCommandRegisterHandler(GdbIo gdbIo, EmulatorHost machine) {
        _gdbIo = gdbIo;
        _machine = machine;
    }

    public string ReadAllRegisters() {
        System.Diagnostics.Debug.WriteLine("Reading all registers");
        StringBuilder response = new(2 * 4 * 16);
        for (int i = 0; i < 16; i++) {
            string regValue = _gdbFormatter.FormatValueAsHex32(GetRegisterValue(i));
            response.Append(regValue);
        }

        return _gdbIo.GenerateResponse(response.ToString());
    }

    public string ReadRegister(string commandContent) {
        try {
            long index = ConvertUtils.ParseHex32(commandContent);
            System.Diagnostics.Debug.WriteLine($"Reading register {index}");
            return _gdbIo.GenerateResponse(_gdbFormatter.FormatValueAsHex32(GetRegisterValue((int)index)));
        } catch (FormatException nfe) {
            System.Diagnostics.Debug.WriteLine($"Register read requested but could not understand the request {commandContent}, {nfe.Message}");
            return _gdbIo.GenerateUnsupportedResponse();
        }
    }

    public string WriteAllRegisters(string commandContent) {
        try {
            byte[] data = ConvertUtils.HexToByteArray(commandContent);
            for (int i = 0; i < data.Length; i += 4) {
                long value = ConvertUtils.BytesToInt32(data, i);
                SetRegisterValue(i / 4, (ushort)value);
            }

            return _gdbIo.GenerateResponse("OK");
        } catch (FormatException nfe) {
            System.Diagnostics.Debug.WriteLine($"Register write requested but could not understand the request {commandContent}, {nfe.Message}");
            return _gdbIo.GenerateUnsupportedResponse();
        }
    }

    public string WriteRegister(string commandContent) {
        String[] split = commandContent.Split("=");
        int registerIndex = (int)ConvertUtils.ParseHex32(split[0]);
        uint registerValue = ConvertUtils.Swap32(ConvertUtils.ParseHex32(split[1]));
        SetRegisterValue(registerIndex, (ushort)registerValue);
        return _gdbIo.GenerateResponse("OK");
    }

    private uint GetRegisterValue(int regIndex) {
        if (regIndex < 8) {
            return regIndex switch
            {
                0 => (uint)_machine.VirtualMachine.Processor.AX,
                1 => (uint)_machine.VirtualMachine.Processor.CX,
                2 => (uint)_machine.VirtualMachine.Processor.DX,
                3 => (uint)_machine.VirtualMachine.Processor.BX,
                4 => _machine.VirtualMachine.Processor.SP,
                5 => _machine.VirtualMachine.Processor.BP,
                6 => _machine.VirtualMachine.Processor.SI,
                7 => _machine.VirtualMachine.Processor.DI,
            };
        }
        if (regIndex == 8) {
            return MemoryUtils.ToPhysicalAddress(_machine.VirtualMachine.Processor.CS, _machine.VirtualMachine.Processor.IP);
        }

        if (regIndex == 9) {
            return (uint)_machine.VirtualMachine.Processor.Flags.Value;
        }

        if (regIndex < 16) {
            var registerIndex = GetSegmentRegisterIndex(regIndex);
            return registerIndex switch
            {
                0 => (uint)_machine.VirtualMachine.Processor.AX,
                1 => (uint)_machine.VirtualMachine.Processor.CX,
                2 => (uint)_machine.VirtualMachine.Processor.DX,
                3 => (uint)_machine.VirtualMachine.Processor.BX,
                4 => _machine.VirtualMachine.Processor.SP,
                5 => _machine.VirtualMachine.Processor.BP,
                6 => _machine.VirtualMachine.Processor.SI,
                7 => _machine.VirtualMachine.Processor.DI,
            };
        }

        return 0;
    }

    private int GetSegmentRegisterIndex(int gdbRegisterIndex)
    {
        int registerIndex = gdbRegisterIndex - 10;
        if (registerIndex < 3)
        {
            return registerIndex + 1;
        }

        if (registerIndex == 3)
        {
            return 0;
        }

        return registerIndex;
    }

    private void SetRegisterValue(int regIndex, ushort value) {
        if (regIndex < 8) {
            switch(regIndex)
            {
                case 0:
                    _machine.VirtualMachine.Processor.AX = (short)value;
                    break;
                case 1:
                    _machine.VirtualMachine.Processor.CX = (short)value;
                    break;
                case 2:
                    _machine.VirtualMachine.Processor.DX = (short)value;
                    break;
                case 3:
                    _machine.VirtualMachine.Processor.BX = (short)value;
                    break;
                case 4:
                    _machine.VirtualMachine.Processor.SP = value;
                    break;
                case 5:
                    _machine.VirtualMachine.Processor.BP = value;
                    break;
                case 6:
                    _machine.VirtualMachine.Processor.SI = value;
                    break;
                case 7:
                    _machine.VirtualMachine.Processor.DI = value;
                    break;
            };
        } else if (regIndex == 8) {
            _machine.VirtualMachine.Processor.IP = value;
        } else if (regIndex == 9) {
            _machine.VirtualMachine.Processor.Flags.Value = (EFlags)value;
        } else if (regIndex < 16) {
            var registerIndex = GetSegmentRegisterIndex(regIndex);
            switch (registerIndex)
            {
                case 0:
                    _machine.VirtualMachine.Processor.AX = (short)value;
                    break;
                case 1:
                    _machine.VirtualMachine.Processor.CX = (short)value;
                    break;
                case 2:
                    _machine.VirtualMachine.Processor.DX = (short)value;
                    break;
                case 3:
                    _machine.VirtualMachine.Processor.BX = (short)value;
                    break;
                case 4:
                    _machine.VirtualMachine.Processor.SP = value;
                    break;
                case 5:
                    _machine.VirtualMachine.Processor.BP = value;
                    break;
                case 6:
                    _machine.VirtualMachine.Processor.SI = value;
                    break;
                case 7:
                    _machine.VirtualMachine.Processor.DI = value;
                    break;
            };
        }
    }
}