using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

using NumericUpDown = Aeon.Emulator.Launcher.NumericUpDown;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class NumericUpDownSteps : IDisposable
{
    private NumericUpDown? control;

    [Given("a new NumericUpDown control is created")]
    public void GivenANewNumericUpDownControlIsCreated()
    {
        control = TestHelpers.RunOnUIThread(() => new NumericUpDown());
    }

    [Then("the NumericUpDown value should be {int}")]
    public void ThenTheNumericUpDownValueShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => control!.Value);
        Assert.AreEqual(expected, actual);
    }

    [Then("the NumericUpDown minimum value should be {int}")]
    public void ThenTheNumericUpDownMinimumValueShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => control!.MinimumValue);
        Assert.AreEqual(expected, actual);
    }

    [Then("the NumericUpDown maximum value should be {int}")]
    public void ThenTheNumericUpDownMaximumValueShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => control!.MaximumValue);
        Assert.AreEqual(expected, actual);
    }

    [Then("the NumericUpDown step value should be {int}")]
    public void ThenTheNumericUpDownStepValueShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => control!.StepValue);
        Assert.AreEqual(expected, actual);
    }

    [Then("the NumericUpDown IsReadOnly should be false")]
    public void ThenTheNumericUpDownIsReadOnlyShouldBeFalse()
    {
        var actual = TestHelpers.RunOnUIThread(() => control!.IsReadOnly);
        Assert.IsFalse(actual);
    }

    [Given("the NumericUpDown value is {int}")]
    public void GivenTheNumericUpDownValueIs(int value)
    {
        TestHelpers.RunOnUIThread(() => control!.Value = value);
    }

    [Given("the NumericUpDown step value is {int}")]
    public void GivenTheNumericUpDownStepValueIs(int step)
    {
        TestHelpers.RunOnUIThread(() => control!.StepValue = step);
    }

    [Given("the NumericUpDown maximum value is {int}")]
    public void GivenTheNumericUpDownMaximumValueIs(int max)
    {
        TestHelpers.RunOnUIThread(() => control!.MaximumValue = max);
    }

    [Given("the NumericUpDown minimum value is {int}")]
    public void GivenTheNumericUpDownMinimumValueIs(int min)
    {
        TestHelpers.RunOnUIThread(() => control!.MinimumValue = min);
    }

    [When("I click the up button")]
    public void WhenIClickTheUpButton()
    {
        TestHelpers.RunOnUIThread(() =>
        {
            control!.Value = Math.Min(control.Value + control.StepValue, control.MaximumValue);
        });
    }

    [When("I click the down button")]
    public void WhenIClickTheDownButton()
    {
        TestHelpers.RunOnUIThread(() =>
        {
            control!.Value = Math.Max(control.Value - control.StepValue, control.MinimumValue);
        });
    }

    [When("I set the NumericUpDown value to {int}")]
    public void WhenISetTheNumericUpDownValueTo(int value)
    {
        TestHelpers.RunOnUIThread(() => control!.Value = value);
    }

    [AfterScenario]
    public void Cleanup()
    {
        control = null;
    }

    public void Dispose()
    {
        Cleanup();
    }
}
