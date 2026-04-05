using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;
using SkiaSharp;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class ClipboardCopySteps
{
    private readonly SharedState state;
    private byte[]? exportedPngBytes;
    private uint[]? decodedPixels;
    private int decodedWidth;
    private int decodedHeight;
    private uint[]? clipboardPixels;
    private int clipboardWidth;
    private int clipboardHeight;

    public ClipboardCopySteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    /// <summary>
    /// Generates the expected BGRA pixel value for a given pixel index
    /// in the 320x200 test pattern. Must exactly match the pattern
    /// written by GivenTheDisplayHasATestPatternRendered.
    /// </summary>
    private static uint ExpectedTestPatternPixel(int index)
    {
        // First 3 pixels are special sentinel values
        if (index == 0) return 0xFF000080; // B=0x80, G=0x00, R=0x00, A=0xFF
        if (index == 1) return 0xFF008000; // B=0x00, G=0x80, R=0x00, A=0xFF
        if (index == 2) return 0xFF800000; // B=0x00, G=0x00, R=0x80, A=0xFF

        byte v = (byte)(index & 0xFF);
        return (uint)(v | ((v / 2) << 8) | ((v / 3) << 16) | (0xFF << 24));
    }

    [Given("no program is loaded in the emulator")]
    public void GivenNoProgramIsLoadedInTheEmulator()
    {
        // Default state: no program loaded - nothing to do
    }

    [Given("the display has a 320x200 test pattern rendered")]
    public void GivenTheDisplayHasATestPatternRendered()
    {
        var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;

        const int width = 320;
        const int height = 200;
        int pixelCount = width * height;

        // Build the expected pixel array
        var pixels = new uint[pixelCount];
        for (int i = 0; i < pixelCount; i++)
        {
            pixels[i] = ExpectedTestPatternPixel(i);
        }

        // Create a WriteableBitmap (for the display Image.Source)
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var fb = bitmap.Lock())
        {
            unsafe
            {
                var span = new Span<uint>(fb.Address.ToPointer(), fb.Size.Width * fb.Size.Height);
                pixels.AsSpan().CopyTo(span);
            }
        }

        // Pass both bitmap and pixel data; the bitmap may not preserve data between Lock() calls
        display.SetRenderTargetForTesting(bitmap, pixels, width, height);
        TestHelpers.Flush();
    }

    [When("I invoke the Copy Screen command")]
    public void WhenIInvokeTheCopyScreenCommand()
    {
        var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
        _ = display.DisplayBitmap;
    }

    [When("I export the display bitmap as PNG bytes")]
    public void WhenIExportTheDisplayBitmapAsPngBytes()
    {
        var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
        exportedPngBytes = display.ExportDisplayAsPngBytes();
    }

    [When("I decode the PNG to pixel data")]
    public void WhenIDecodeThePngToPixelData()
    {
        Assert.IsNotNull(exportedPngBytes, "No PNG bytes to decode");
        Assert.IsTrue(exportedPngBytes.Length > 0, "PNG bytes are empty");

        DecodePngToPixels(exportedPngBytes, out decodedPixels, out decodedWidth, out decodedHeight);
    }

    [When("I invoke the actual Copy Screen clipboard command")]
    public void WhenIInvokeTheActualCopyScreenClipboardCommand()
    {
        // Call the actual CopyToClipboardAsync flow on the MainWindow
        // This is the same code path as Edit > Copy Screen
        var task = Window.CopyToClipboardAsync();
        // Pump the dispatcher to let the async work complete
        Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    [When("I read the clipboard bitmap data")]
    public void WhenIReadTheClipboardBitmapData()
    {
        var clipboard = Window.Clipboard;
        Assert.IsNotNull(clipboard, "Clipboard is null — headless platform may not support clipboard");

#pragma warning disable CS0618 // Using deprecated clipboard API for compatibility
        var formatsTask = clipboard.GetFormatsAsync();
        Dispatcher.UIThread.RunJobs();
        var formats = formatsTask.GetAwaiter().GetResult();

        byte[]? pngBytes = null;

        // Try known PNG format names
        string[] pngFormatNames = ["image/png", "PNG", "png"];
        foreach (var formatName in pngFormatNames)
        {
            if (formats.Contains(formatName))
            {
                var dataTask = clipboard.GetDataAsync(formatName);
                Dispatcher.UIThread.RunJobs();
                var data = dataTask.GetAwaiter().GetResult();
                if (data is byte[] bytes && bytes.Length > 0)
                {
                    pngBytes = bytes;
                    break;
                }
            }
        }
#pragma warning restore CS0618

        Assert.IsNotNull(pngBytes, $"Clipboard does not contain PNG data. Available formats: [{string.Join(", ", formats)}]");
        Assert.IsTrue(pngBytes.Length > 0, "Clipboard PNG bytes are empty");

        DecodePngToPixels(pngBytes, out clipboardPixels, out clipboardWidth, out clipboardHeight);
    }

    [Then("the PNG bytes should not be empty")]
    public void ThenThePngBytesShouldNotBeEmpty()
    {
        Assert.IsNotNull(exportedPngBytes, "ExportDisplayAsPngBytes returned null — no render target available.");
        Assert.IsTrue(exportedPngBytes.Length > 0, "PNG bytes are empty (0 bytes).");
    }

    [Then("the PNG should decode to a 320x200 image")]
    public void ThenThePngShouldDecodeTo320x200()
    {
        Assert.IsNotNull(exportedPngBytes);
        using var skBitmap = SKBitmap.Decode(exportedPngBytes);
        Assert.IsNotNull(skBitmap, "Failed to decode PNG via SkiaSharp");
        Assert.AreEqual(320, skBitmap.Width, "PNG width mismatch");
        Assert.AreEqual(200, skBitmap.Height, "PNG height mismatch");
    }

    [Then(@"pixel (\d+),(\d+) should have BGRA value (0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+)")]
    public void ThenPixelShouldHaveBGRAValue(int x, int y, string bHex, string gHex, string rHex, string aHex)
    {
        Assert.IsNotNull(decodedPixels, "Pixel data not decoded — run 'I decode the PNG to pixel data' first");
        AssertPixelBGRA(decodedPixels, decodedWidth, x, y, bHex, gHex, rHex, aHex);
    }

    [Then("not all pixels should be zero")]
    public void ThenNotAllPixelsShouldBeZero()
    {
        Assert.IsNotNull(decodedPixels, "No pixel data available");
        bool anyNonZero = decodedPixels.Any(px => px != 0);
        Assert.IsTrue(anyNonZero, "All pixels are zero (black) — the bitmap was not rendered");
    }

    [Then("every pixel in the decoded bitmap should match the test pattern")]
    public void ThenEveryPixelShouldMatchTheTestPattern()
    {
        Assert.IsNotNull(decodedPixels, "Pixel data not decoded");
        Assert.AreEqual(320, decodedWidth, "Decoded bitmap width mismatch");
        Assert.AreEqual(200, decodedHeight, "Decoded bitmap height mismatch");

        int totalPixels = decodedWidth * decodedHeight;
        int mismatchCount = 0;
        int firstMismatchIndex = -1;

        for (int i = 0; i < totalPixels; i++)
        {
            uint expected = ExpectedTestPatternPixel(i);
            uint actual = decodedPixels[i];
            if (expected != actual)
            {
                if (firstMismatchIndex == -1)
                    firstMismatchIndex = i;
                mismatchCount++;
            }
        }

        if (mismatchCount > 0)
        {
            int x = firstMismatchIndex % decodedWidth;
            int y = firstMismatchIndex / decodedWidth;
            uint expected = ExpectedTestPatternPixel(firstMismatchIndex);
            uint actual = decodedPixels[firstMismatchIndex];
            Assert.Fail(
                $"{mismatchCount} of {totalPixels} pixels do not match the test pattern. " +
                $"First mismatch at pixel ({x},{y}) index {firstMismatchIndex}: " +
                $"expected 0x{expected:X8}, got 0x{actual:X8}");
        }
    }

    [Then("the clipboard should contain image data")]
    public void ThenTheClipboardShouldContainImageData()
    {
        var clipboard = Window.Clipboard;
        Assert.IsNotNull(clipboard, "Clipboard is null");

#pragma warning disable CS0618 // Using deprecated clipboard API for compatibility
        var formatsTask = clipboard.GetFormatsAsync();
        Dispatcher.UIThread.RunJobs();
        var formats = formatsTask.GetAwaiter().GetResult();
#pragma warning restore CS0618

        Assert.IsTrue(formats.Length > 0,
            "Clipboard has no data formats after Copy Screen command");

        // The clipboard should contain at least one image format
        bool hasImageFormat = formats.Any(f =>
            f.Contains("png", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("image", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("bitmap", StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(hasImageFormat,
            $"Clipboard does not contain any image format. Available: [{string.Join(", ", formats)}]");
    }

    [Then("the clipboard bitmap should be 320x200")]
    public void ThenTheClipboardBitmapShouldBe320x200()
    {
        Assert.IsNotNull(clipboardPixels, "No clipboard pixel data");
        Assert.AreEqual(320, clipboardWidth, "Clipboard bitmap width mismatch");
        Assert.AreEqual(200, clipboardHeight, "Clipboard bitmap height mismatch");
    }

    [Then(@"clipboard pixel (\d+),(\d+) should have BGRA value (0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+),(0x[0-9A-Fa-f]+)")]
    public void ThenClipboardPixelShouldHaveBGRAValue(int x, int y, string bHex, string gHex, string rHex, string aHex)
    {
        Assert.IsNotNull(clipboardPixels, "Clipboard pixel data not decoded");
        AssertPixelBGRA(clipboardPixels, clipboardWidth, x, y, bHex, gHex, rHex, aHex);
    }

    [Then("not all clipboard pixels should be zero")]
    public void ThenNotAllClipboardPixelsShouldBeZero()
    {
        Assert.IsNotNull(clipboardPixels, "No clipboard pixel data");
        bool anyNonZero = clipboardPixels.Any(px => px != 0);
        Assert.IsTrue(anyNonZero, "All clipboard pixels are zero (black) — clipboard copy produced a blank image");
    }

    private static void AssertPixelBGRA(uint[] pixels, int width, int x, int y, string bHex, string gHex, string rHex, string aHex)
    {
        byte expectedB = Convert.ToByte(bHex, 16);
        byte expectedG = Convert.ToByte(gHex, 16);
        byte expectedR = Convert.ToByte(rHex, 16);
        byte expectedA = Convert.ToByte(aHex, 16);

        int idx = y * width + x;
        Assert.IsTrue(idx < pixels.Length, $"Pixel ({x},{y}) out of range");

        uint pixel = pixels[idx];
        byte actualB = (byte)(pixel & 0xFF);
        byte actualG = (byte)((pixel >> 8) & 0xFF);
        byte actualR = (byte)((pixel >> 16) & 0xFF);
        byte actualA = (byte)((pixel >> 24) & 0xFF);

        Assert.AreEqual(expectedB, actualB, $"Pixel ({x},{y}) Blue channel mismatch");
        Assert.AreEqual(expectedG, actualG, $"Pixel ({x},{y}) Green channel mismatch");
        Assert.AreEqual(expectedR, actualR, $"Pixel ({x},{y}) Red channel mismatch");
        Assert.AreEqual(expectedA, actualA, $"Pixel ({x},{y}) Alpha channel mismatch");
    }

    private static void DecodePngToPixels(byte[] pngBytes, out uint[] pixels, out int width, out int height)
    {
        using var skBitmap = SKBitmap.Decode(pngBytes);
        Assert.IsNotNull(skBitmap, "Failed to decode PNG via SkiaSharp");
        width = skBitmap.Width;
        height = skBitmap.Height;
        pixels = new uint[width * height];

        for (int y2 = 0; y2 < height; y2++)
        {
            for (int x2 = 0; x2 < width; x2++)
            {
                var color = skBitmap.GetPixel(x2, y2);
                pixels[y2 * width + x2] =
                    (uint)(color.Blue | (color.Green << 8) | (color.Red << 16) | (color.Alpha << 24));
            }
        }
    }
}
