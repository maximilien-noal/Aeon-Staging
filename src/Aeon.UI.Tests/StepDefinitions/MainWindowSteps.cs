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
        var actual = TestHelpers.RunOnUIThread(() => Window.Width);
        Assert.AreEqual(width, (int)actual);
    }

    [Then("the window height should be {int}")]
    public void ThenTheWindowHeightShouldBe(int height)
    {
        var actual = TestHelpers.RunOnUIThread(() => Window.Height);
        Assert.AreEqual(height, (int)actual);
    }

    [Then("the window minimum width should be {int}")]
    public void ThenTheWindowMinimumWidthShouldBe(int minWidth)
    {
        var actual = TestHelpers.RunOnUIThread(() => Window.MinWidth);
        Assert.AreEqual(minWidth, (int)actual);
    }

    [Then("the window minimum height should be {int}")]
    public void ThenTheWindowMinimumHeightShouldBe(int minHeight)
    {
        var actual = TestHelpers.RunOnUIThread(() => Window.MinHeight);
        Assert.AreEqual(minHeight, (int)actual);
    }

    [Then("the main menu should have {int} items")]
    public void ThenTheMainMenuShouldHaveItems(int count)
    {
        var actual = TestHelpers.RunOnUIThread(() =>
        {
            var menu = Window.FindControl<Menu>("mainMenu")!;
            return TestHelpers.GetMenuItems(menu).Count;
        });
        Assert.AreEqual(count, actual);
    }

    [Then("the main menu should contain a {string} menu")]
    public void ThenTheMainMenuShouldContainAMenu(string header)
    {
        var found = TestHelpers.RunOnUIThread(() =>
        {
            var menu = Window.FindControl<Menu>("mainMenu")!;
            return TestHelpers.GetMenuItems(menu).Any(mi => (string?)mi.Header == header);
        });
        Assert.IsTrue(found, $"Menu '{header}' not found.");
    }

    [When("I open the {string} menu")]
    public void WhenIOpenTheMenu(string header)
    {
        currentMenuItems = TestHelpers.RunOnUIThread(() =>
        {
            var menu = Window.FindControl<Menu>("mainMenu")!;
            var topItem = TestHelpers.GetMenuItems(menu).First(mi => (string?)mi.Header == header);
            topItem.Open();
            return topItem.Items.OfType<MenuItem>().ToList();
        });
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
        var isChecked = TestHelpers.RunOnUIThread(() => item.IsChecked);
        Assert.IsTrue(isChecked, $"Menu item '{header}' should be checked.");
    }

    [Then("the toolbar should be visible")]
    public void ThenTheToolbarShouldBeVisible()
    {
        var visible = TestHelpers.RunOnUIThread(() => Window.FindControl<Border>("toolBar")!.IsVisible);
        Assert.IsTrue(visible);
    }

    [Then("the toolbar should contain a {string} toggle button")]
    public void ThenTheToolbarShouldContainAToggleButton(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => Window.FindControl<ToggleButton>(name) != null);
        Assert.IsTrue(exists, $"ToggleButton '{name}' not found.");
    }

    [Then("the toolbar should contain a {string} button")]
    public void ThenTheToolbarShouldContainAButton(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => Window.FindControl<Button>(name) != null);
        Assert.IsTrue(exists, $"Button '{name}' not found.");
    }

    [Then("the toolbar should contain a {string} text block")]
    public void ThenTheToolbarShouldContainATextBlock(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => Window.FindControl<TextBlock>(name) != null);
        Assert.IsTrue(exists, $"TextBlock '{name}' not found.");
    }

    [Then("the speed label should display {string}")]
    public void ThenTheSpeedLabelShouldDisplay(string expected)
    {
        var actual = TestHelpers.RunOnUIThread(() => Window.FindControl<TextBlock>("speedLabel")!.Text);
        Assert.AreEqual(expected, actual);
    }

    [Then("the {string} control should exist")]
    public void ThenTheControlShouldExist(string name)
    {
        var exists = TestHelpers.RunOnUIThread(() => Window.FindControl<Control>(name) != null);
        Assert.IsTrue(exists, $"Control '{name}' not found.");
    }

    [Then("the menu container should be visible")]
    public void ThenTheMenuContainerShouldBeVisible()
    {
        var visible = TestHelpers.RunOnUIThread(() => Window.FindControl<StackPanel>("menuContainer")!.IsVisible);
        Assert.IsTrue(visible);
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (state.MainWindow != null)
        {
            TestHelpers.RunOnUIThread(() => state.MainWindow.Close());
            state.MainWindow = null;
        }
    }
}
