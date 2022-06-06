namespace Aeon.Emulator.Gdb;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

public class GdbServer : IDisposable {
    private EventWaitHandle? _waitHandle;
    private bool _disposedValue;
    private readonly EmulatorHost _machine;
    private bool _isRunning = true;
    private int _gdbPortNumber;
    private Thread? _gdbServerThread;

    public GdbServer(EmulatorHost machine, int gdbPortNumber) {
        this._machine = machine;
        if (gdbPortNumber is not 0) {
            _gdbPortNumber = gdbPortNumber;
            _gdbServerThread = new(RunServer){
                Name = "GdbServer"
            };
            Start();
        }
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                _gdbServerThread?.Join();
                _isRunning = false;
            }
            _disposedValue = true;
        }
    }

    public GdbCommandHandler? GdbCommandHandler { get; private set; }

    private void AcceptOneConnection(GdbIo gdbIo) {
        var gdbCommandHandler = new GdbCommandHandler(gdbIo, _machine);
        gdbCommandHandler.PauseEmulator();
        _waitHandle?.Set();
        GdbCommandHandler = gdbCommandHandler;
        while (gdbCommandHandler.IsConnected && gdbIo.IsClientConnected) {
            string command = gdbIo.ReadCommand();
            if (!string.IsNullOrWhiteSpace(command)) {
                gdbCommandHandler.RunCommand(command);
            }
        }
        System.Diagnostics.Debug.WriteLine("Client disconnected");
    }

    private void RunServer() {
        if(_gdbPortNumber is 0) {
            return;
        }
        int port = _gdbPortNumber;
        System.Diagnostics.Debug.WriteLine("Starting GDB server");
        try {
            while (_isRunning) {
                try {
                    using var gdbIo = new GdbIo(port);
                    AcceptOneConnection(gdbIo);
                } catch (IOException e) {
                    System.Diagnostics.Debug.WriteLine("Error in the GDB server, restarting it...");
                }
            }
        } catch (Exception e) {
            System.Diagnostics.Debug.WriteLine("Error in the GDB server, restarting it...");
        } finally {
            _machine.Halt();
            _machine.MachineBreakpoints.PauseHandler.RequestResume();
            System.Diagnostics.Debug.WriteLine("GDB server stopped");
        }
    }

    private void Start() {
        _gdbServerThread?.Start();
        // wait for thread to start
        _waitHandle = new AutoResetEvent(false);
        _waitHandle.WaitOne(Timeout.Infinite);
        _waitHandle.Dispose();
    }
}