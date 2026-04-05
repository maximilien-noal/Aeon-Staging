using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

namespace Aeon.UI.Tests.Support;

/// <summary>
/// Minimal Avalonia application for headless testing.
/// Mirrors the real App's theme and resource setup.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        this.Styles.Add(new FluentTheme());
        // Load the real app's styles from the Aeon assembly
        this.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Aeon"))
        {
            Source = new Uri("avares://Aeon/EmulatorDisplayStyles.axaml")
        });
        this.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Aeon"))
        {
            Source = new Uri("avares://Aeon/TaskDialogStyles.axaml")
        });

        // Add the backgroundGradient resource matching App.axaml
        this.Resources["backgroundGradient"] = new LinearGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.Parse("SteelBlue"), 0),
                new GradientStop(Color.Parse("#102040"), 1)
            }
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the headless Avalonia application.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
