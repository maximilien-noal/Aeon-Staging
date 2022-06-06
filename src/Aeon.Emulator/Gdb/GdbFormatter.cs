namespace Aeon.Emulator.Gdb;

using Aeon.Emulator.Utils;

public class GdbFormatter {

    public string FormatValueAsHex32(uint value) {
        return $"{ConvertUtils.Swap32(value):X8}";
    }

    public string FormatValueAsHex8(byte value) {
        return $"{ConvertUtils.Uint8(value):X2}";
    }
}