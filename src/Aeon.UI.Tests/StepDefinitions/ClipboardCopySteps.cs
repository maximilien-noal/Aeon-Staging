using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Aeon.Emulator;
using Aeon.Emulator.Configuration;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class ClipboardCopySteps : IDisposable
{
    private readonly SharedState state;
    private Exception? caughtException;
    private string? tempDir;

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

    [When("I invoke the Copy Screen command")]
    public void WhenIInvokeTheCopyScreenCommand()
    {
        caughtException = null;
        try
        {
            var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
            _ = display.DisplayBitmap;
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
    }

    [Then("no exception should be thrown")]
    public void ThenNoExceptionShouldBeThrown()
    {
        Assert.IsNull(caughtException, $"Exception was thrown: {caughtException?.Message}");
    }

    [Given("a VGA mode 13h pattern program has been loaded and executed")]
    public void GivenAVGAMode13hPatternProgramHasBeenLoadedAndExecuted()
    {
        var comBytes = TestHelpers.GetVgaPatternComBytes();
        tempDir = TestHelpers.CreateTempDirWithComFile("VGA.COM", comBytes);

        var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
        var config = AeonConfiguration.GetQuickLaunchConfiguration(tempDir, "VGA.COM");
        var host = EmulatorHost.CreateWithConfig(config);
        display.EmulatorHost = host;
        TestHelpers.Flush();

        // Let the emulator run briefly to render
        Thread.Sleep(500);
    }

    [Then("the display bitmap should not be null")]
    public void ThenTheDisplayBitmapShouldNotBeNull()
    {
        var bitmap = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!.DisplayBitmap;
        Assert.IsNotNull(bitmap);
    }

    [AfterScenario]
    public void Cleanup()
    {
        TestHelpers.CleanupTempDir(tempDir);
        tempDir = null;
    }

    public void Dispose()
    {
        Cleanup();
    }
}
