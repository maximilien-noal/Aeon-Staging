using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class ClipboardCopySteps
{
    private readonly SharedState state;
    private Exception? copyException;
    private Bitmap? copiedBitmap;
    private string? tempDir;

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
        this.copiedBitmap = null;

        try
        {
            await this.Window.CopyToClipboardAsync();
            this.copiedBitmap = this.Window.CreateClipboardBitmap();
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

    [Then("the copied bitmap should match the display bitmap exactly")]
    public void ThenTheCopiedBitmapShouldMatchTheDisplayBitmapExactly()
    {
        Assert.IsNull(this.copyException, this.copyException?.ToString());

        var display = this.Window.FindControl<EmulatorDisplay>("emulatorDisplay");
        Assert.IsNotNull(display);
        Assert.IsNotNull(display.DisplayBitmap, "Display bitmap is null.");
        Assert.IsNotNull(this.copiedBitmap, "Clipboard bitmap is null.");

        using var expectedStream = new MemoryStream();
        using var actualStream = new MemoryStream();
        display.DisplayBitmap!.Save(expectedStream);
        this.copiedBitmap!.Save(actualStream);

        CollectionAssert.AreEqual(expectedStream.ToArray(), actualStream.ToArray(), "Copied bitmap content differs from display bitmap.");
    }

    [AfterScenario]
    public void Cleanup()
    {
        this.copiedBitmap?.Dispose();
        this.copiedBitmap = null;

        TestHelpers.CleanupTempDir(this.tempDir);
        this.tempDir = null;
    }
}
