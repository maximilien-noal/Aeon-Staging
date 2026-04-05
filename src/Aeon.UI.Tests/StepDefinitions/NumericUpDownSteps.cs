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
        control = new NumericUpDown();
        TestHelpers.Flush();
    }

    [Then("the NumericUpDown value should be {int}")]
    public void ThenTheNumericUpDownValueShouldBe(int expected)
    {
        Assert.AreEqual(expected, control!.Value);
    }

    [Then("the NumericUpDown minimum value should be {int}")]
    public void ThenTheNumericUpDownMinimumValueShouldBe(int expected)
    {
        Assert.AreEqual(expected, control!.MinimumValue);
    }

    [Then("the NumericUpDown maximum value should be {int}")]
    public void ThenTheNumericUpDownMaximumValueShouldBe(int expected)
    {
        Assert.AreEqual(expected, control!.MaximumValue);
    }

    [Then("the NumericUpDown step value should be {int}")]
    public void ThenTheNumericUpDownStepValueShouldBe(int expected)
    {
        Assert.AreEqual(expected, control!.StepValue);
    }

    [Then("the NumericUpDown IsReadOnly should be false")]
    public void ThenTheNumericUpDownIsReadOnlyShouldBeFalse()
    {
        Assert.IsFalse(control!.IsReadOnly);
    }

    [Given("the NumericUpDown value is {int}")]
    public void GivenTheNumericUpDownValueIs(int value)
    {
        control!.Value = value;
        TestHelpers.Flush();
    }

    [Given("the NumericUpDown step value is {int}")]
    public void GivenTheNumericUpDownStepValueIs(int step)
    {
        control!.StepValue = step;
        TestHelpers.Flush();
    }

    [Given("the NumericUpDown maximum value is {int}")]
    public void GivenTheNumericUpDownMaximumValueIs(int max)
    {
        control!.MaximumValue = max;
        TestHelpers.Flush();
    }

    [Given("the NumericUpDown minimum value is {int}")]
    public void GivenTheNumericUpDownMinimumValueIs(int min)
    {
        control!.MinimumValue = min;
        TestHelpers.Flush();
    }

    [When("I click the up button")]
    public void WhenIClickTheUpButton()
    {
        control!.Value = Math.Min(control.Value + control.StepValue, control.MaximumValue);
        TestHelpers.Flush();
    }

    [When("I click the down button")]
    public void WhenIClickTheDownButton()
    {
        control!.Value = Math.Max(control.Value - control.StepValue, control.MinimumValue);
        TestHelpers.Flush();
    }

    [When("I set the NumericUpDown value to {int}")]
    public void WhenISetTheNumericUpDownValueTo(int value)
    {
        control!.Value = value;
        TestHelpers.Flush();
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
