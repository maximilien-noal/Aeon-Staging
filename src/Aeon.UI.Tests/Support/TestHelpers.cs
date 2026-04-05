using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Aeon.UI.Tests.Support;

/// <summary>
/// Helper methods for Avalonia headless testing.
/// After <see cref="AvaloniaHooks.EnsureInitialized"/>, UI objects can
/// be created directly on the current (test) thread.
/// Call <see cref="Flush"/> to pump the dispatcher when needed.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Pumps the Avalonia dispatcher so pending jobs (layout, bindings,
    /// timers queued during the current tick) are processed.
    /// Silently catches rendering errors (e.g. missing fonts in headless mode).
    /// </summary>
    public static void Flush()
    {
        try
        {
            Dispatcher.UIThread.RunJobs();
        }
        catch (InvalidOperationException)
        {
            // Headless mode may fail rendering (e.g. FluentIcons glyphs).
        }
    }

    /// <summary>
    /// Creates and shows a window in headless mode, returning it.
    /// </summary>
    public static T ShowWindow<T>() where T : Window, new()
    {
        var window = new T();
        window.Show();
        Flush();
        return window;
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
}
