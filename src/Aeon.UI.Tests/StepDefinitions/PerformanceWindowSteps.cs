using Avalonia.Controls;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class PerformanceWindowSteps : IDisposable
{
    private PerformanceWindow? window;

    [Given("the performance window is open")]
    public void GivenThePerformanceWindowIsOpen()
    {
        window = TestHelpers.ShowWindow<PerformanceWindow>();
    }

    [Then("the performance window should be visible")]
    public void ThenThePerformanceWindowShouldBeVisible()
    {
        Assert.IsTrue(window!.IsVisible);
    }

    [Then("the {string} expander should exist")]
    public void ThenTheExpanderShouldExist(string name)
    {
        Assert.IsTrue(window!.FindControl<Expander>(name) != null, $"Expander '{name}' not found.");
    }

    [Then("the {string} expander should be expanded")]
    public void ThenTheExpanderShouldBeExpanded(string name)
    {
        Assert.IsTrue(window!.FindControl<Expander>(name)!.IsExpanded, $"Expander '{name}' should be expanded.");
    }

    [Then("the {string} text block should exist in the processor expander")]
    public void ThenTheTextBlockShouldExistInTheProcessorExpander(string name)
    {
        Assert.IsTrue(window!.FindControl<TextBlock>(name) != null, $"TextBlock '{name}' not found in processor expander.");
    }

    [Then("the {string} text block should exist in the memory expander")]
    public void ThenTheTextBlockShouldExistInTheMemoryExpander(string name)
    {
        Assert.IsTrue(window!.FindControl<TextBlock>(name) != null, $"TextBlock '{name}' not found in memory expander.");
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (window != null)
        {
            window.Close();
            TestHelpers.Flush();
            window = null;
        }
    }

    public void Dispose()
    {
        Cleanup();
    }
}
