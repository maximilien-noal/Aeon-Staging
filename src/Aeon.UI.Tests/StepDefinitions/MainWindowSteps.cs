using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class MainWindowSteps
{
    private readonly SharedState state;
    private List<MenuItem>? currentMenuItems;

    public MainWindowSteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    [Given("the main window is open")]
    public void GivenTheMainWindowIsOpen()
    {
        if (state.MainWindow != null)
            return;
        state.MainWindow = TestHelpers.ShowWindow<MainWindow>();
    }

    [Then("the window width should be {int}")]
    public void ThenTheWindowWidthShouldBe(int width)
    {
        Assert.AreEqual(width, (int)Window.Width);
    }

    [Then("the window height should be {int}")]
    public void ThenTheWindowHeightShouldBe(int height)
    {
        Assert.AreEqual(height, (int)Window.Height);
    }

    [Then("the window minimum width should be {int}")]
    public void ThenTheWindowMinimumWidthShouldBe(int minWidth)
    {
        Assert.AreEqual(minWidth, (int)Window.MinWidth);
    }

    [Then("the window minimum height should be {int}")]
    public void ThenTheWindowMinimumHeightShouldBe(int minHeight)
    {
        Assert.AreEqual(minHeight, (int)Window.MinHeight);
    }

    [Then("the main menu should have {int} items")]
    public void ThenTheMainMenuShouldHaveItems(int count)
    {
        var menu = Window.FindControl<Menu>("mainMenu")!;
        var actual = menu.Items.OfType<MenuItem>().Count();
        Assert.AreEqual(count, actual);
    }

    [Then("the main menu should contain a {string} menu")]
    public void ThenTheMainMenuShouldContainAMenu(string header)
    {
        var menu = Window.FindControl<Menu>("mainMenu")!;
        var found = menu.Items.OfType<MenuItem>().Any(mi => (string?)mi.Header == header);
        Assert.IsTrue(found, $"Menu '{header}' not found.");
    }

    [When("I open the {string} menu")]
    public void WhenIOpenTheMenu(string header)
    {
        var menu = Window.FindControl<Menu>("mainMenu")!;
        var topItem = menu.Items.OfType<MenuItem>().First(mi => (string?)mi.Header == header);
        topItem.Open();
        TestHelpers.Flush();
        currentMenuItems = topItem.Items.OfType<MenuItem>().ToList();
    }

    [Then("the menu should contain the following items")]
    public void ThenTheMenuShouldContainTheFollowingItems(DataTable table)
    {
        Assert.IsNotNull(currentMenuItems);
        var expectedHeaders = table.Rows.Select(r => r["Header"]).ToList();
        var actualHeaders = currentMenuItems.Select(mi => (string?)mi.Header).ToList();
        foreach (var expected in expectedHeaders)
        {
            Assert.IsTrue(actualHeaders.Contains(expected), $"Menu item '{expected}' not found. Actual: {string.Join(", ", actualHeaders)}");
        }
    }

    [Then("the {string} menu item should be checked")]
    public void ThenTheMenuItemShouldBeChecked(string header)
    {
        Assert.IsNotNull(currentMenuItems);
        var item = currentMenuItems.FirstOrDefault(mi => (string?)mi.Header == header);
        Assert.IsNotNull(item, $"Menu item '{header}' not found.");
        Assert.IsTrue(item.IsChecked, $"Menu item '{header}' should be checked.");
    }

    [Then("the toolbar should be visible")]
    public void ThenTheToolbarShouldBeVisible()
    {
        Assert.IsTrue(Window.FindControl<Border>("toolBar")!.IsVisible);
    }

    [Then("the toolbar should contain a {string} toggle button")]
    public void ThenTheToolbarShouldContainAToggleButton(string name)
    {
        Assert.IsTrue(Window.FindControl<ToggleButton>(name) != null, $"ToggleButton '{name}' not found.");
    }

    [Then("the toolbar should contain a {string} button")]
    public void ThenTheToolbarShouldContainAButton(string name)
    {
        Assert.IsTrue(Window.FindControl<Button>(name) != null, $"Button '{name}' not found.");
    }

    [Then("the toolbar should contain a {string} text block")]
    public void ThenTheToolbarShouldContainATextBlock(string name)
    {
        Assert.IsTrue(Window.FindControl<TextBlock>(name) != null, $"TextBlock '{name}' not found.");
    }

    [Then("the speed label should display {string}")]
    public void ThenTheSpeedLabelShouldDisplay(string expected)
    {
        Assert.AreEqual(expected, Window.FindControl<TextBlock>("speedLabel")!.Text);
    }

    [Then("the {string} control should exist")]
    public void ThenTheControlShouldExist(string name)
    {
        Assert.IsTrue(Window.FindControl<Control>(name) != null, $"Control '{name}' not found.");
    }

    [Then("the menu container should be visible")]
    public void ThenTheMenuContainerShouldBeVisible()
    {
        Assert.IsTrue(Window.FindControl<StackPanel>("menuContainer")!.IsVisible);
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (state.MainWindow != null)
        {
            state.MainWindow.Close();
            TestHelpers.Flush();
            state.MainWindow = null;
        }
    }
}
