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
        Assert.IsTrue(dialog!.IsVisible);
    }

    [Then("the palette dialog title should be {string}")]
    public void ThenThePaletteDialogTitleShouldBe(string expected)
    {
        Assert.AreEqual(expected, dialog!.Title);
    }

    [Then("the palette grid should have {int} rows")]
    public void ThenThePaletteGridShouldHaveRows(int expected)
    {
        Assert.AreEqual(expected, dialog!.FindControl<UniformGrid>("grid")!.Rows);
    }

    [Then("the palette grid should have {int} columns")]
    public void ThenThePaletteGridShouldHaveColumns(int expected)
    {
        Assert.AreEqual(expected, dialog!.FindControl<UniformGrid>("grid")!.Columns);
    }

    [Then("the palette grid should contain {int} color rectangles")]
    public void ThenThePaletteGridShouldContainColorRectangles(int expected)
    {
        var grid = dialog!.FindControl<UniformGrid>("grid")!;
        Assert.AreEqual(expected, grid.Children.OfType<Rectangle>().Count());
    }

    [Then("the palette dialog width should be {int}")]
    public void ThenThePaletteDialogWidthShouldBe(int expected)
    {
        Assert.AreEqual(expected, (int)dialog!.Width);
    }

    [Then("the palette dialog height should be {int}")]
    public void ThenThePaletteDialogHeightShouldBe(int expected)
    {
        Assert.AreEqual(expected, (int)dialog!.Height);
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (dialog != null)
        {
            dialog.Close();
            TestHelpers.Flush();
            dialog = null;
        }
    }

    public void Dispose()
    {
        Cleanup();
    }
}
