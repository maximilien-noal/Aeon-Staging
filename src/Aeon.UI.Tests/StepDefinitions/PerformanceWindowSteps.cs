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
        var visible = TestHelpers.RunOnUIThread(() => window!.IsVisible);
        Assert.IsTrue(visible);
    }

    [Then("the {string} expander should exist")]
    public void ThenTheExpanderShouldExist(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => window!.FindControl<Expander>(name) != null);
        Assert.IsTrue(exists, $"Expander '{name}' not found.");
    }

    [Then("the {string} expander should be expanded")]
    public void ThenTheExpanderShouldBeExpanded(string name)
    {
        var expanded = TestHelpers.RunOnUIThread(() => window!.FindControl<Expander>(name)!.IsExpanded);
        Assert.IsTrue(expanded, $"Expander '{name}' should be expanded.");
    }

    [Then("the {string} text block should exist in the processor expander")]
    public void ThenTheTextBlockShouldExistInTheProcessorExpander(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => window!.FindControl<TextBlock>(name) != null);
        Assert.IsTrue(exists, $"TextBlock '{name}' not found in processor expander.");
    }

    [Then("the {string} text block should exist in the memory expander")]
    public void ThenTheTextBlockShouldExistInTheMemoryExpander(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => window!.FindControl<TextBlock>(name) != null);
        Assert.IsTrue(exists, $"TextBlock '{name}' not found in memory expander.");
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (window != null)
        {
            TestHelpers.RunOnUIThread(() => window.Close());
            window = null;
        }
    }

    public void Dispose()
    {
        Cleanup();
    }
}
