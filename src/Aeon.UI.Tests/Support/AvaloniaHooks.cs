using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Threading;
using Reqnroll;

namespace Aeon.UI.Tests.Support;

/// <summary>
/// Reqnroll hooks for Avalonia headless platform lifecycle.
/// </summary>
[Binding]
public sealed class AvaloniaHooks
{
    private static IDisposable? session;

    [BeforeTestRun]
    public static void InitializeAvalonia()
    {
        session = HeadlessUnitTestSession.StartNew(typeof(TestApp));
    }

    [AfterTestRun]
    public static void TearDownAvalonia()
    {
        session?.Dispose();
    }
}
