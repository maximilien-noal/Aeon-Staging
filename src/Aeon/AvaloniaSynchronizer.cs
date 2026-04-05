using Avalonia.Threading;

namespace Aeon.Emulator.Launcher;

/// <summary>
/// Allows Aeon to raise events via an Avalonia Dispatcher object.
/// </summary>
internal sealed class AvaloniaSynchronizer : IEventSynchronizer
{
    /// <summary>
    /// Invokes a method on the UI thread asynchronously.
    /// </summary>
    /// <param name="method">Method to invoke.</param>
    /// <param name="source">The object which raised the event.</param>
    /// <param name="e">Arguments to pass to the method.</param>
    public void BeginInvoke(Delegate method, object source, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => method.DynamicInvoke(source, e));
    }
}
