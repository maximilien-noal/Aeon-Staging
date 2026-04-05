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
        var actual = TestHelpers.RunOnUIThread(() => Window.WindowState.ToString());
        Assert.AreEqual(expected, actual);
    }

    [When("I toggle full screen mode")]
    public void WhenIToggleFullScreenMode()
    {
        TestHelpers.RunOnUIThread(() =>
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
                Window.Background = (IBrush?)Window.FindResource("backgroundGradient") ?? savedBackground ?? Brushes.Transparent;
            }
        });
    }

    [Given("the window is in full screen mode")]
    public void GivenTheWindowIsInFullScreenMode()
    {
        WhenIToggleFullScreenMode();
    }

    [Then("the menu container should not be visible")]
    public void ThenTheMenuContainerShouldNotBeVisible()
    {
        var visible = TestHelpers.RunOnUIThread(() =>
            Window.FindControl<StackPanel>("menuContainer")!.IsVisible);
        Assert.IsFalse(visible);
    }

    [Then("the window background should be black")]
    public void ThenTheWindowBackgroundShouldBeBlack()
    {
        var isBlack = TestHelpers.RunOnUIThread(() =>
        {
            var brush = Window.Background as ISolidColorBrush;
            return brush != null && brush.Color == Colors.Black;
        });
        Assert.IsTrue(isBlack);
    }

    [Then("the window background should not be black")]
    public void ThenTheWindowBackgroundShouldNotBeBlack()
    {
        var isBlack = TestHelpers.RunOnUIThread(() =>
        {
            var brush = Window.Background as ISolidColorBrush;
            return brush != null && brush.Color == Colors.Black;
        });
        Assert.IsFalse(isBlack);
    }
}
