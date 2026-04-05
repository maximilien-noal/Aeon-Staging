using Aeon.Emulator;
using Aeon.Emulator.Launcher;
using Aeon.Emulator.Video.Rendering;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class EmulatorDisplaySteps : IDisposable
{
    private EmulatorDisplay? display;

    [Given("a new EmulatorDisplay control is created")]
    public void GivenANewEmulatorDisplayControlIsCreated()
    {
        display = new EmulatorDisplay();
        TestHelpers.Flush();
    }

    [Then("the EmulatorState should be {string}")]
    public void ThenTheEmulatorStateShouldBe(string expected)
    {
        Assert.AreEqual(expected, display!.EmulatorState.ToString());
    }

    [Then("the MouseInputMode should be {string}")]
    public void ThenTheMouseInputModeShouldBe(string expected)
    {
        Assert.AreEqual(expected, display!.MouseInputMode.ToString());
    }

    [Then("IsMouseCursorCaptured should be false")]
    public void ThenIsMouseCursorCapturedShouldBeFalse()
    {
        Assert.IsFalse(display!.IsMouseCursorCaptured);
    }

    [Then("the EmulationSpeed should be {int}")]
    public void ThenTheEmulationSpeedShouldBe(int expected)
    {
        Assert.AreEqual(expected, display!.EmulationSpeed);
    }

    [Then("IsAspectRatioLocked should be true")]
    public void ThenIsAspectRatioLockedShouldBeTrue()
    {
        Assert.IsTrue(display!.IsAspectRatioLocked);
    }

    [Then("IsAspectRatioLocked should be false")]
    public void ThenIsAspectRatioLockedShouldBeFalse()
    {
        Assert.IsFalse(display!.IsAspectRatioLocked);
    }

    [When("I set the MouseInputMode to {string}")]
    public void WhenISetTheMouseInputModeTo(string mode)
    {
        var parsed = Enum.Parse<MouseInputMode>(mode);
        display!.MouseInputMode = parsed;
        TestHelpers.Flush();
    }

    [Given("the MouseInputMode is set to {string}")]
    public void GivenTheMouseInputModeIsSetTo(string mode)
    {
        var parsed = Enum.Parse<MouseInputMode>(mode);
        display!.MouseInputMode = parsed;
        TestHelpers.Flush();
    }

    [When("I set the EmulationSpeed to {int}")]
    public void WhenISetTheEmulationSpeedTo(int speed)
    {
        display!.EmulationSpeed = speed;
        TestHelpers.Flush();
    }

    [When("I set IsAspectRatioLocked to false")]
    public void WhenISetIsAspectRatioLockedToFalse()
    {
        display!.IsAspectRatioLocked = false;
        TestHelpers.Flush();
    }

    [Then("the ScalingAlgorithm should be {string}")]
    public void ThenTheScalingAlgorithmShouldBe(string expected)
    {
        Assert.AreEqual(expected, display!.ScalingAlgorithm.ToString());
    }

    [AfterScenario]
    public void Cleanup()
    {
        display = null;
    }

    public void Dispose()
    {
        Cleanup();
    }
}
