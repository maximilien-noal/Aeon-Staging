using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aeon.UI.Tests;

[TestClass]
public sealed class ClipboardCopyTests
{
    [TestMethod]
    public async Task CopyScreenWhenNoProgramLoadedDoesNotThrow()
    {
        AvaloniaHooks.EnsureInitialized();
        var window = TestHelpers.ShowWindow<MainWindow>();

        try
        {
            await window.CopyToClipboardAsync();
        }
        finally
        {
            window.Close();
            TestHelpers.Flush();
        }
    }

    [TestMethod]
    public async Task CopyScreenMatchesDisplayBitmapExactlyAfterVgaRendering()
    {
        AvaloniaHooks.EnsureInitialized();
        var window = TestHelpers.ShowWindow<MainWindow>();
        string? tempDir = null;

        try
        {
            var comBytes = TestHelpers.GetVgaPatternComBytes();
            tempDir = TestHelpers.CreateTempDirWithComFile("vga_pattern.com", comBytes);
            var comPath = Path.Combine(tempDir, "vga_pattern.com");

            var quickLaunch = typeof(MainWindow).GetMethod("QuickLaunch", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(quickLaunch, "Could not find MainWindow.QuickLaunch method.");
            quickLaunch.Invoke(window, [comPath]);

            var display = window.FindControl<EmulatorDisplay>("emulatorDisplay");
            Assert.IsNotNull(display);

            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (display.DisplayBitmap == null && DateTime.UtcNow < timeout)
            {
                TestHelpers.Flush();
                await Task.Delay(50);
            }

            Assert.IsNotNull(display.DisplayBitmap, "Display bitmap was not initialized after running VGA pattern program.");

            using var expectedStream = new MemoryStream();
            display.DisplayBitmap!.Save(expectedStream);

            await window.CopyToClipboardAsync();

            using var copiedBitmap = window.CreateClipboardBitmap();
            Assert.IsNotNull(copiedBitmap, "Clipboard bitmap is null.");

            using var actualStream = new MemoryStream();
            copiedBitmap.Save(actualStream);

            CollectionAssert.AreEqual(expectedStream.ToArray(), actualStream.ToArray(), "Copied bitmap content differs from display bitmap.");
        }
        finally
        {
            window.Close();
            TestHelpers.Flush();
            TestHelpers.CleanupTempDir(tempDir);
        }
    }
}
