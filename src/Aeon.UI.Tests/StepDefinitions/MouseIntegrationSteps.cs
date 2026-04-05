using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class MouseIntegrationSteps
{
    private readonly SharedState state;

    public MouseIntegrationSteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    [Then("the mouse input mode should be {string}")]
    public void ThenTheMouseInputModeShouldBe(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<EmulatorDisplay>("emulatorDisplay")!.MouseInputMode.ToString());
        Assert.AreEqual(expected, actual);
    }

    [Then("the mouse integration button should not be checked")]
    public void ThenTheMouseIntegrationButtonShouldNotBeChecked()
    {
        var isChecked = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<ToggleButton>("mouseIntegrationButton")!.IsChecked);
        Assert.AreNotEqual(true, isChecked);
    }

    [Then("the mouse integration button should be checked")]
    public void ThenTheMouseIntegrationButtonShouldBeChecked()
    {
        var isChecked = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<ToggleButton>("mouseIntegrationButton")!.IsChecked);
        Assert.IsTrue(isChecked == true);
    }

    [When("I click the mouse integration button")]
    public void WhenIClickTheMouseIntegrationButton()
    {
        TestHelpers.RunOnUIThread(() =>
        {
            var button = Window.FindControl<ToggleButton>("mouseIntegrationButton")!;
            button.IsChecked = button.IsChecked != true;
            var display = Window.FindControl<EmulatorDisplay>("emulatorDisplay")!;
            display.MouseInputMode = button.IsChecked == true
                ? MouseInputMode.Absolute
                : MouseInputMode.Relative;
        });
    }
}
