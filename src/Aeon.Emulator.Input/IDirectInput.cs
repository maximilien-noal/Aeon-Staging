﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Aeon.Emulator.Input
{
    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectInput8Inst
    {
        public unsafe DirectInput8V* Vtbl;
    }

    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DirectInput8V
    {
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, uint> Release;

        //internal unsafe delegate uint CreateDeviceProc(IntPtr pThis, Guid* rguid, DirectInputDevice8Inst** lplpDirectInputDevice, IntPtr pUnkOuter);
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, Guid*, DirectInputDevice8Inst**, void*, uint> CreateDevice;

        //internal delegate uint EnumDevicesProc(IntPtr pThis, uint dwDevType, [MarshalAs(UnmanagedType.FunctionPtr)] EnumDevicesCallback lpCallback, IntPtr pvRef, uint dwFlags);
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, uint, delegate* unmanaged[Stdcall]<DIDEVICEINSTANCE*, IntPtr, uint>, IntPtr, uint, uint> EnumDevices;

        //public IntPtr CreateDevice;
        //public IntPtr EnumDevices;
        public IntPtr GetDeviceStatus;
        public IntPtr RunControlPanel;
        //public IntPtr Initialize;

        //internal delegate uint InitializeInputProc(IntPtr pThis, IntPtr hinst, uint dwVersion);
        public delegate* unmanaged[Stdcall]<DirectInput8Inst*, IntPtr, uint, uint> Initialize;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate uint NoParamProc(IntPtr pThis);

    //[UnmanagedFunctionPointer(CallingConvention.StdCall)]
    //internal delegate uint InitializeInputProc(IntPtr pThis, IntPtr hinst, uint dwVersion);
    //[UnmanagedFunctionPointer(CallingConvention.StdCall)]
    //internal delegate uint EnumDevicesProc(IntPtr pThis, uint dwDevType, [MarshalAs(UnmanagedType.FunctionPtr)] EnumDevicesCallback lpCallback, IntPtr pvRef, uint dwFlags);
    //[UnmanagedFunctionPointer(CallingConvention.StdCall)]
    //internal unsafe delegate uint EnumDevicesCallback(IntPtr lpddi, IntPtr pvRef);
    //[UnmanagedFunctionPointer(CallingConvention.StdCall)]
    //internal unsafe delegate uint CreateDeviceProc(IntPtr pThis, Guid* rguid, DirectInputDevice8Inst** lplpDirectInputDevice, IntPtr pUnkOuter);

    [SupportedOSPlatform("windows")]
    public enum DeviceClass
    {
        All = 0,
        Other = 1,
        Pointer = 2,
        Keyboard = 3,
        GameController = 4
    }

    [SupportedOSPlatform("windows")]
    [Flags]
    public enum DeviceEnumFlags
    {
        All = 0,
        AttachedOnly = 0x00000001,
        ForceFeedback = 0x00000100,
        IncludeAliases = 0x00010000,
        IncludePhantoms = 0x00020000,
        IncludeHidden = 0x00040000
    }

    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct DIDEVICEINSTANCE
    {
        public uint dwSize;
        public Guid guidInstance;
        public Guid guidProduct;
        public uint dwDevType;
        public unsafe fixed char wszInstanceName[260];
        public unsafe fixed char wszProductName[260];
        public Guid guidFFDriver;
        public ushort wUsagePage;
        public ushort wUsage;
    }
}
