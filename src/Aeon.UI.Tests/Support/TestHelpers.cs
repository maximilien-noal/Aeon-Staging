using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Aeon.UI.Tests.Support;

/// <summary>
/// Helper methods for Avalonia headless testing.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Runs an action on the Avalonia UI thread and waits for completion.
    /// </summary>
    public static T RunOnUIThread<T>(Func<T> action)
    {
        return Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs an action on the Avalonia UI thread and waits for completion.
    /// </summary>
    public static void RunOnUIThread(Action action)
    {
        Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs an async action on the Avalonia UI thread and waits for completion.
    /// </summary>
    public static async Task RunOnUIThreadAsync(Func<Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <summary>
    /// Creates and shows a window in headless mode, returning it.
    /// </summary>
    public static T ShowWindow<T>() where T : Window, new()
    {
        return RunOnUIThread(() =>
        {
            var window = new T();
            window.Show();
            return window;
        });
    }

    /// <summary>
    /// Creates a temporary directory with a test COM file.
    /// Returns the path to the temp directory.
    /// </summary>
    public static string CreateTempDirWithComFile(string comFileName, byte[] comFileBytes)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aeon-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllBytes(Path.Combine(tempDir, comFileName), comFileBytes);
        return tempDir;
    }

    /// <summary>
    /// Cleans up a temporary directory.
    /// </summary>
    public static void CleanupTempDir(string? path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            try { Directory.Delete(path, recursive: true); }
            catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Gets the raw bytes of the VGA mode 13h test COM program.
    /// </summary>
    public static byte[] GetVgaPatternComBytes()
    {
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "vga_pattern.com");
        return File.ReadAllBytes(testDataPath);
    }

    /// <summary>
    /// Finds a named control inside a window.
    /// </summary>
    public static T? FindControl<T>(Control parent, string name) where T : Control
    {
        return parent.FindControl<T>(name);
    }

    /// <summary>
    /// Gets all menu items from a Menu control.
    /// </summary>
    public static List<MenuItem> GetMenuItems(Menu menu)
    {
        return menu.Items.OfType<MenuItem>().ToList();
    }
}
