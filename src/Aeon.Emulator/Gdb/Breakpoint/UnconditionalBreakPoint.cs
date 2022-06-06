﻿using System;

namespace Aeon.Emulator.Gdb.Breakpoint;

public class UnconditionalBreakPoint : BreakPoint {

    public UnconditionalBreakPoint(BreakPointType breakPointType, Action<BreakPoint> onReached, bool removeOnTrigger) : base(breakPointType, onReached, removeOnTrigger) {
    }

    public override bool Matches(long address) {
        return true;
    }

    public override bool Matches(long startAddress, long endAddress) {
        return true;
    }
}