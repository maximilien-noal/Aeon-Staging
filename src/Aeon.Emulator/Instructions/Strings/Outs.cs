﻿using System.Runtime.CompilerServices;

namespace Aeon.Emulator.Instructions.Strings
{
    internal static class Outs
    {
        [Opcode("6E", Name = "outsb", AddressSize = 16, OperandSize = 16 | 32)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OutByte(VirtualMachine vm)
        {
            if (vm.Processor.RepeatPrefix == RepeatPrefix.None)
                OutSingleByte(vm);
            else
                OutBytes(vm);

            vm.Processor.InstructionEpilog();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OutSingleByte(VirtualMachine vm)
        {
            var srcBase = vm.Processor.GetOverrideBase(SegmentIndex.DS);
            byte src = vm.PhysicalMemory.GetByte(srcBase + vm.Processor.SI);
            vm.WritePortByte((ushort)vm.Processor.DX, src);

            if (!vm.Processor.Flags.Direction)
                vm.Processor.SI++;
            else
                vm.Processor.SI--;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OutBytes(VirtualMachine vm)
        {
            if (vm.Processor.CX != 0)
            {
                OutSingleByte(vm);
                vm.Processor.EIP -= (ushort)(1 + vm.Processor.PrefixCount);
                vm.Processor.CX--;
            }
        }

        [Alternate("OutByte", AddressSize = 32, OperandSize = 16 | 32)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OutByte32(VirtualMachine vm)
        {
            if (vm.Processor.RepeatPrefix == RepeatPrefix.None)
                OutSingleByte32(vm);
            else
                OutBytes32(vm);

            vm.Processor.InstructionEpilog();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OutSingleByte32(VirtualMachine vm)
        {
            var srcBase = vm.Processor.GetOverrideBase(SegmentIndex.DS);
            byte src = vm.PhysicalMemory.GetByte(srcBase + vm.Processor.ESI);
            vm.WritePortByte((ushort)vm.Processor.DX, src);

            if (!vm.Processor.Flags.Direction)
                vm.Processor.ESI++;
            else
                vm.Processor.ESI--;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OutBytes32(VirtualMachine vm)
        {
            if (vm.Processor.ECX != 0)
            {
                OutSingleByte32(vm);
                vm.Processor.EIP -= 1 + vm.Processor.PrefixCount;
                vm.Processor.ECX--;
            }
        }
    }
}
