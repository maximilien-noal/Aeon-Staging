using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Aeon.Emulator.Launcher;
using Aeon.UI.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace Aeon.UI.Tests.StepDefinitions;

[Binding]
public sealed class FullScreenSteps
{
    private readonly SharedState state;
    private IBrush? savedBackground;

    public FullScreenSteps(SharedState state)
    {
        this.state = state;
    }

    private MainWindow Window => state.MainWindow!;

    [Then("the window state should be {string}")]
    public void ThenTheWindowStateShouldBe(string expected)
    {
        Assert.AreEqual(expected, Window.WindowState.ToString());
    }

    [When("I toggle full screen mode")]
    public void WhenIToggleFullScreenMode()
    {
        if (Window.WindowState != Avalonia.Controls.WindowState.FullScreen)
        {
            savedBackground ??= Window.Background;
            Window.FindControl<StackPanel>("menuContainer")!.IsVisible = false;
            Window.WindowState = Avalonia.Controls.WindowState.FullScreen;
            Window.Background = Brushes.Black;
        }
        else
        {
            Window.FindControl<StackPanel>("menuContainer")!.IsVisible = true;
            Window.WindowState = Avalonia.Controls.WindowState.Normal;
            var resource = Window.FindResource("backgroundGradient");
            Window.Background = resource is IBrush brush ? brush : savedBackground ?? Brushes.Transparent;
        }
        TestHelpers.Flush();
    }

    [Given("the window is in full screen mode")]
    public void GivenTheWindowIsInFullScreenMode()
    {
        WhenIToggleFullScreenMode();
    }

    [Then("the menu container should not be visible")]
    public void ThenTheMenuContainerShouldNotBeVisible()
    {
        Assert.IsFalse(Window.FindControl<StackPanel>("menuContainer")!.IsVisible);
    }

    [Then("the window background should be black")]
    public void ThenTheWindowBackgroundShouldBeBlack()
    {
        var brush = Window.Background as ISolidColorBrush;
        Assert.IsTrue(brush != null && brush.Color == Colors.Black);
    }

    [Then("the window background should not be black")]
    public void ThenTheWindowBackgroundShouldNotBeBlack()
    {
        var brush = Window.Background as ISolidColorBrush;
        Assert.IsFalse(brush != null && brush.Color == Colors.Black);
    }
}
