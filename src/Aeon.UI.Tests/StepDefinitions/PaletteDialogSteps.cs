using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class PaletteDialogSteps : IDisposable
{
    private PaletteDialog? dialog;

    [Given("the palette dialog is open")]
    public void GivenThePaletteDialogIsOpen()
    {
        dialog = TestHelpers.ShowWindow<PaletteDialog>();
    }

    [Then("the palette dialog should be visible")]
    public void ThenThePaletteDialogShouldBeVisible()
    {
        var visible = TestHelpers.RunOnUIThread(() => dialog!.IsVisible);
        Assert.IsTrue(visible);
    }

    [Then("the palette dialog title should be {string}")]
    public void ThenThePaletteDialogTitleShouldBe(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => dialog!.Title);
        Assert.AreEqual(expected, actual);
    }

    [Then("the palette grid should have {int} rows")]
    public void ThenThePaletteGridShouldHaveRows(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => dialog!.FindControl<UniformGrid>("grid")!.Rows);
        Assert.AreEqual(expected, actual);
    }

    [Then("the palette grid should have {int} columns")]
    public void ThenThePaletteGridShouldHaveColumns(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => dialog!.FindControl<UniformGrid>("grid")!.Columns);
        Assert.AreEqual(expected, actual);
    }

    [Then("the palette grid should contain {int} color rectangles")]
    public void ThenThePaletteGridShouldContainColorRectangles(int expected)
    {
        var count = TestHelpers.RunOnUIThread(() =>
        {
            var grid = dialog!.FindControl<UniformGrid>("grid")!;
            return grid.Children.OfType<Rectangle>().Count();
        });
        Assert.AreEqual(expected, count);
    }

    [Then("the palette dialog width should be {int}")]
    public void ThenThePaletteDialogWidthShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => dialog!.Width);
        Assert.AreEqual(expected, (int)actual);
    }

    [Then("the palette dialog height should be {int}")]
    public void ThenThePaletteDialogHeightShouldBe(int expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => dialog!.Height);
        Assert.AreEqual(expected, (int)actual);
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (dialog != null)
        {
            TestHelpers.RunOnUIThread(() => dialog.Close());
            dialog = null;
        }
    }

    public void Dispose()
    {
        Cleanup();
    }
}
