using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Aeon.Emulator.Launcher;
using Aeon.Emulator.Launcher.Behaviors;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class ClipboardCopySteps
{
    private readonly SharedState state;
    private Exception? copyException;
    private Bitmap? clipboardBitmap;
    private string? tempDir;
    private byte[]? expectedPixels;
    private PixelSize expectedSize;

    public ClipboardCopySteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => this.state.MainWindow!;

    [Given("no program is loaded in the emulator")]
    public void GivenNoProgramIsLoadedInTheEmulator()
    {
        var display = this.Window.FindControl<EmulatorDisplay>("emulatorDisplay");
        Assert.IsNotNull(display);
        Assert.IsNull(display.EmulatorHost);
    }

    [Given("a VGA mode 13h pattern program has been loaded and executed")]
    public async Task GivenAVgaModePatternProgramHasBeenLoadedAndExecuted()
    {
        var comBytes = TestHelpers.GetVgaPatternComBytes();
        this.tempDir = TestHelpers.CreateTempDirWithComFile("vga_pattern.com", comBytes);
        var comPath = Path.Combine(this.tempDir, "vga_pattern.com");

        var quickLaunch = typeof(MainWindow).GetMethod("QuickLaunch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(quickLaunch, "Could not find MainWindow.QuickLaunch method.");
        quickLaunch.Invoke(this.Window, [comPath]);

        var display = this.Window.FindControl<EmulatorDisplay>("emulatorDisplay");
        Assert.IsNotNull(display);

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (display.DisplayBitmap == null && DateTime.UtcNow < timeout)
        {
            TestHelpers.Flush();
            await Task.Delay(50);
        }

        Assert.IsNotNull(display.DisplayBitmap, "Display bitmap was not initialized after running VGA pattern program.");
    }

    [When("I invoke the Copy Screen command")]
    public async Task WhenIInvokeTheCopyScreenCommand()
    {
        this.copyException = null;
        this.clipboardBitmap?.Dispose();
        this.clipboardBitmap = null;
        this.expectedPixels = null;

        try
        {
            var display = this.Window.FindControl<EmulatorDisplay>("emulatorDisplay");
            Assert.IsNotNull(display);

            var imageControl = display.GetVisualDescendants()
                .OfType<Image>()
                .FirstOrDefault(img => img.Name == "displayImage");

            Assert.IsNotNull(imageControl?.Source, "displayImage has no source bitmap.");
            var sourceBitmap = (Bitmap)imageControl!.Source!;

            this.expectedSize = sourceBitmap.PixelSize;
            this.expectedPixels = ExtractPixels(sourceBitmap);

            await CopyScreenToClipboardBehavior.CopyToClipboardAsync(sourceBitmap, this.Window.Clipboard!);
            this.clipboardBitmap = await TryReadClipboardBitmapAsync(this.Window.Clipboard);
        }
        catch (Exception ex)
        {
            this.copyException = ex;
        }
    }

    [Then("no exception should be thrown")]
    public void ThenNoExceptionShouldBeThrown()
    {
        if (this.copyException != null)
            Assert.Fail(this.copyException.ToString());
    }

    [Then("the clipboard bitmap should match the display bitmap exactly")]
    public void ThenTheClipboardBitmapShouldMatchTheDisplayBitmapExactly()
    {
        Assert.IsNull(this.copyException, this.copyException?.ToString());
        Assert.IsNotNull(this.clipboardBitmap, "Clipboard bitmap is null.");
        Assert.IsNotNull(this.expectedPixels, "Expected display pixels are null.");

        Assert.AreEqual(this.expectedSize.Width, this.clipboardBitmap.PixelSize.Width, "Clipboard width differs from display width.");
        Assert.AreEqual(this.expectedSize.Height, this.clipboardBitmap.PixelSize.Height, "Clipboard height differs from display height.");

        var actualPixels = ExtractPixels(this.clipboardBitmap);
        CollectionAssert.AreEqual(this.expectedPixels, actualPixels, "Clipboard bitmap pixels differ from display bitmap pixels.");
    }

    [AfterScenario]
    public void Cleanup()
    {
        this.clipboardBitmap?.Dispose();
        this.clipboardBitmap = null;

        TestHelpers.CleanupTempDir(this.tempDir);
        this.tempDir = null;
    }

    private static unsafe byte[] ExtractPixels(Bitmap bitmap)
    {
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        var bytesPerRow = width * 4;
        var pixels = new byte[bytesPerRow * height];

        fixed (byte* buffer = pixels)
        {
            bitmap.CopyPixels(new PixelRect(0, 0, width, height), (IntPtr)buffer, pixels.Length, bytesPerRow);
        }

        return pixels;
    }

    private static async Task<Bitmap?> TryReadClipboardBitmapAsync(IClipboard? clipboard)
    {
        if (clipboard == null)
            return null;

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < timeout)
        {
            var fromExtension = await clipboard.TryGetBitmapAsync();
            if (fromExtension != null)
                return fromExtension;

            var formats = await clipboard.GetFormatsAsync();
            foreach (var format in formats.OrderByDescending(IsImageFormat))
            {
                var data = await clipboard.GetDataAsync(format);
                var bitmap = ConvertClipboardDataToBitmap(data);
                if (bitmap != null)
                    return bitmap;
            }

            await Task.Delay(50);
        }

        return null;
    }

    private static Bitmap? ConvertClipboardDataToBitmap(object? data)
    {
        if (data is Bitmap bitmap)
            return bitmap;

        if (data is byte[] bytes && bytes.Length > 0)
        {
            try
            {
                return new Bitmap(new MemoryStream(bytes));
            }
            catch
            {
                return null;
            }
        }

        if (data is Stream stream)
        {
            try
            {
                using var memory = new MemoryStream();
                if (stream.CanSeek)
                    stream.Position = 0;
                stream.CopyTo(memory);
                memory.Position = 0;
                return new Bitmap(memory);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsImageFormat(string format)
    {
        return format.Contains("image", StringComparison.OrdinalIgnoreCase)
            || format.Contains("png", StringComparison.OrdinalIgnoreCase)
            || format.Contains("bitmap", StringComparison.OrdinalIgnoreCase)
            || format.Contains("dib", StringComparison.OrdinalIgnoreCase);
    }
}
