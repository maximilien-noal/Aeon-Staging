using System;

using Aeon.Avalonia.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Aeon.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static MainWindow? MainWindow { get; private set; }

    public static string[] Args { get; private set; } = Array.Empty<string>();

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = MainWindow = new MainWindow();
            Args = desktop.Args;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
