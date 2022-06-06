namespace Aeon.Emulator.Gdb;

using System;
using System.Threading;

public class PauseHandler : IDisposable {

    private volatile bool _paused;

    private volatile bool _pauseEnded;

    private volatile bool _pauseRequested;
    private bool disposedValue;
    private readonly ManualResetEvent _manualResetEvent = new(true);

    public void RequestPause() {
        _pauseRequested = true;
    }

    public void RequestPauseAndWait() {
        _pauseRequested = true;
        _manualResetEvent.WaitOne(Timeout.Infinite);
    }

    public void RequestResume() {
        _pauseRequested = false;
        _manualResetEvent.Set();
    }

    public void WaitIfPaused() {
        while (_pauseRequested) {
            _paused = true;
            Await();
        }

        _paused = false;
        _pauseEnded = true;
    }

    private void Await() {
        try {
            _manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(1));
        } catch (AbandonedMutexException exception) {
            Thread.CurrentThread.Interrupt();
            throw new InvalidOperationException($"Fatal error while waiting paused in {nameof(Await)}", exception);
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                _manualResetEvent.Dispose();
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