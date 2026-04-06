using Avalonia.Threading;

namespace Aeon.Emulator.Launcher;

internal sealed class AvaloniaSynchronizer : IEventSynchronizer
{
    public void BeginInvoke(Delegate method, object source, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (method is EventHandler eh)
                eh(source, e);
            else if (method is EventHandler<MouseMoveEventArgs> mmh)
                mmh(source, (MouseMoveEventArgs)e);
            else if (method is EventHandler<ErrorEventArgs> errh)
                errh(source, (ErrorEventArgs)e);
            else
                throw new ArgumentException($"Unsupported delegate type: {method.GetType().Name}", nameof(method));
        });
    }
}
