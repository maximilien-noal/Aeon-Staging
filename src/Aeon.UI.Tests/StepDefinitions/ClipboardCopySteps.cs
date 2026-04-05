using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    public ClipboardCopySteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    [Given("no program is loaded in the emulator")]
    public void GivenNoProgramIsLoadedInTheEmulator()
    {
        // Default state: no program loaded - nothing to do
    }

    [Given("the display has a 320x200 test pattern rendered")]
    public void GivenTheDisplayHasATestPatternRendered()
    {
        var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;

        var bitmap = new WriteableBitmap(
            new PixelSize(320, 200),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var fb = bitmap.Lock())
        {
            unsafe
            {
                var span = new Span<uint>(fb.Address.ToPointer(), fb.Size.Width * fb.Size.Height);
                for (int i = 0; i < span.Length; i++)
                {
                    byte v = (byte)(i & 0xFF);
                    span[i] = (uint)(v | ((v / 2) << 8) | ((v / 3) << 16) | (0xFF << 24));
                }
                // pixel(0,0) = B=0x80, G=0x00, R=0x00, A=0xFF
                span[0] = 0xFF000080;
                // pixel(1,0) = B=0x00, G=0x80, R=0x00, A=0xFF
                span[1] = 0xFF008000;
                // pixel(2,0) = B=0x00, G=0x00, R=0x80, A=0xFF
                span[2] = 0xFF800000;
            }
        }

        display.SetRenderTargetForTesting(bitmap);
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

        using var skBitmap = SKBitmap.Decode(exportedPngBytes);
        Assert.IsNotNull(skBitmap, "Failed to decode PNG via SkiaSharp");
        decodedWidth = skBitmap.Width;
        decodedHeight = skBitmap.Height;
        decodedPixels = new uint[decodedWidth * decodedHeight];

        for (int y = 0; y < decodedHeight; y++)
        {
            for (int x = 0; x < decodedWidth; x++)
            {
                var color = skBitmap.GetPixel(x, y);
                decodedPixels[y * decodedWidth + x] =
                    (uint)(color.Blue | (color.Green << 8) | (color.Red << 16) | (color.Alpha << 24));
            }
        }
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

        byte expectedB = Convert.ToByte(bHex, 16);
        byte expectedG = Convert.ToByte(gHex, 16);
        byte expectedR = Convert.ToByte(rHex, 16);
        byte expectedA = Convert.ToByte(aHex, 16);

        int idx = y * decodedWidth + x;
        Assert.IsTrue(idx < decodedPixels.Length, $"Pixel ({x},{y}) out of range");

        uint pixel = decodedPixels[idx];
        byte actualB = (byte)(pixel & 0xFF);
        byte actualG = (byte)((pixel >> 8) & 0xFF);
        byte actualR = (byte)((pixel >> 16) & 0xFF);
        byte actualA = (byte)((pixel >> 24) & 0xFF);

        Assert.AreEqual(expectedB, actualB, $"Pixel ({x},{y}) Blue channel mismatch");
        Assert.AreEqual(expectedG, actualG, $"Pixel ({x},{y}) Green channel mismatch");
        Assert.AreEqual(expectedR, actualR, $"Pixel ({x},{y}) Red channel mismatch");
        Assert.AreEqual(expectedA, actualA, $"Pixel ({x},{y}) Alpha channel mismatch");
    }

    [Then("not all pixels should be zero")]
    public void ThenNotAllPixelsShouldBeZero()
    {
        Assert.IsNotNull(decodedPixels, "No pixel data available");
        bool anyNonZero = decodedPixels.Any(px => px != 0);
        Assert.IsTrue(anyNonZero, "All pixels are zero (black) — the bitmap was not rendered");
    }
}
