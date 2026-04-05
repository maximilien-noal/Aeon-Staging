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
        display = TestHelpers.RunOnUIThread(() => new EmulatorDisplay());
    }

    [Then("the EmulatorState should be {string}")]
    public void ThenTheEmulatorStateShouldBe(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.EmulatorState.ToString());
        Assert.AreEqual(expected, actual);
    }

    [Then("the MouseInputMode should be {string}")]
    public void ThenTheMouseInputModeShouldBe(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.MouseInputMode.ToString());
        Assert.AreEqual(expected, actual);
    }

    [Then("IsMouseCursorCaptured should be false")]
    public void ThenIsMouseCursorCapturedShouldBeFalse()
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.IsMouseCursorCaptured);
        Assert.IsFalse(actual);
    }

    [Then("the EmulationSpeed should be {int}")]
    public void ThenTheEmulationSpeedShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.EmulationSpeed);
        Assert.AreEqual(expected, actual);
    }

    [Then("IsAspectRatioLocked should be true")]
    public void ThenIsAspectRatioLockedShouldBeTrue()
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.IsAspectRatioLocked);
        Assert.IsTrue(actual);
    }

    [Then("IsAspectRatioLocked should be false")]
    public void ThenIsAspectRatioLockedShouldBeFalse()
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.IsAspectRatioLocked);
        Assert.IsFalse(actual);
    }

    [When("I set the MouseInputMode to {string}")]
    public void WhenISetTheMouseInputModeTo(string mode)
    {
        var parsed = Enum.Parse<MouseInputMode>(mode);
        TestHelpers.RunOnUIThread(() => display!.MouseInputMode = parsed);
    }

    [Given("the MouseInputMode is set to {string}")]
    public void GivenTheMouseInputModeIsSetTo(string mode)
    {
        var parsed = Enum.Parse<MouseInputMode>(mode);
        TestHelpers.RunOnUIThread(() => display!.MouseInputMode = parsed);
    }

    [When("I set the EmulationSpeed to {int}")]
    public void WhenISetTheEmulationSpeedTo(int speed)
    {
        TestHelpers.RunOnUIThread(() => display!.EmulationSpeed = speed);
    }

    [When("I set IsAspectRatioLocked to false")]
    public void WhenISetIsAspectRatioLockedToFalse()
    {
        TestHelpers.RunOnUIThread(() => display!.IsAspectRatioLocked = false);
    }

    [Then("the ScalingAlgorithm should be {string}")]
    public void ThenTheScalingAlgorithmShouldBe(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => display!.ScalingAlgorithm.ToString());
        Assert.AreEqual(expected, actual);
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
