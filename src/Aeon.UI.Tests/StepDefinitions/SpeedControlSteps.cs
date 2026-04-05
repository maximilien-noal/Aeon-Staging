using Avalonia.Controls;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class SpeedControlSteps
{
    private readonly SharedState state;

    public SpeedControlSteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    [Given("the default emulation speed is {int}")]
    public void GivenTheDefaultEmulationSpeedIs(int speed)
    {
        var actual = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<EmulatorDisplay>("emulatorDisplay")!.EmulationSpeed);
        Assert.AreEqual(speed, actual);
    }

    [Given("the emulation speed is set to {int}")]
    public void GivenTheEmulationSpeedIsSetTo(int speed)
    {
        TestHelpers.RunOnUIThread(() =>
        {
            var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
            display.EmulationSpeed = speed;
            Window.FindControl<TextBlock>("speedLabel")!.Text = FormatSpeed(speed);
            Window.FindControl<Button>("slowerButton")!.IsEnabled = speed > Aeon.Emulator.EmulatorHost.MinimumSpeed;
            Window.FindControl<Button>("fasterButton")!.IsEnabled = true;
        });
    }

    [When("I click the faster button")]
    public void WhenIClickTheFasterButton()
    {
        TestHelpers.RunOnUIThread(() =>
        {
            var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
            var speedLabel = Window.FindControl<TextBlock>("speedLabel")!;
            var slower = Window.FindControl<Button>("slowerButton")!;
            int newSpeed = display.EmulationSpeed + 100_000;
            display.EmulationSpeed = newSpeed;
            speedLabel.Text = FormatSpeed(newSpeed);
            slower.IsEnabled = newSpeed > Aeon.Emulator.EmulatorHost.MinimumSpeed;
        });
    }

    [When("I click the slower button")]
    public void WhenIClickTheSlowerButton()
    {
        TestHelpers.RunOnUIThread(() =>
        {
            var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
            var speedLabel = Window.FindControl<TextBlock>("speedLabel")!;
            var slower = Window.FindControl<Button>("slowerButton")!;
            int newSpeed = Math.Max(Aeon.Emulator.EmulatorHost.MinimumSpeed, display.EmulationSpeed - 100_000);
            display.EmulationSpeed = newSpeed;
            speedLabel.Text = FormatSpeed(newSpeed);
            slower.IsEnabled = newSpeed > Aeon.Emulator.EmulatorHost.MinimumSpeed;
        });
    }

    [When("I click the faster button {int} times")]
    public void WhenIClickTheFasterButtonTimes(int count)
    {
        for (int i = 0; i < count; i++)
            WhenIClickTheFasterButton();
    }

    [When("I click the slower button {int} times")]
    public void WhenIClickTheSlowerButtonTimes(int count)
    {
        for (int i = 0; i < count; i++)
            WhenIClickTheSlowerButton();
    }

    [Then("the emulation speed should be {int}")]
    public void ThenTheEmulationSpeedShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<EmulatorDisplay>("emulatorDisplay")!.EmulationSpeed);
        Assert.AreEqual(expected, actual);
    }

    [Then("the slower button should be disabled")]
    public void ThenTheSlowerButtonShouldBeDisabled()
    {
        var enabled = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<Button>("slowerButton")!.IsEnabled);
        Assert.IsFalse(enabled);
    }

    [Then("the faster button should be enabled")]
    public void ThenTheFasterButtonShouldBeEnabled()
    {
        var enabled = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<Button>("fasterButton")!.IsEnabled);
        Assert.IsTrue(enabled);
    }

    private static string FormatSpeed(int speed)
    {
        var mhz = (decimal)speed / 1_000_000;
        return mhz.ToString("0.#") + "MHz";
    }
}
