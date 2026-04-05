using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Reqnroll;

namespace Aeon.UI.Tests.Support;

/// <summary>
/// Reqnroll hooks for Avalonia headless platform lifecycle.
/// Avalonia is initialised once per test run via SetupWithoutStarting(),
/// which lets us create controls directly on the test thread.
/// </summary>
[Binding]
public sealed class AvaloniaHooks
{
    private static bool initialized;
    private static readonly object initLock = new();

    /// <summary>
    /// Ensures the Avalonia headless platform is initialised exactly once.
    /// Must be called on the thread that will create UI objects.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        lock (initLock)
        {
            if (!initialized)
            {
                TestApp.BuildAvaloniaApp()
                    .SetupWithoutStarting();
                initialized = true;
            }
        }
    }

    [BeforeTestRun]
    public static void InitializeAvalonia()
    {
        EnsureInitialized();
    }
}
