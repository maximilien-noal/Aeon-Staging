using System;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MainWindow = Aeon.Emulator.Launcher.Views.MainWindow;

namespace Aeon.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static MainWindow? MainWindow { get; private set; }

    public static ReadOnlyCollection<string> Args { get; private set; } = new(Array.Empty<string>());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = MainWindow = new MainWindow();
            Args = new(desktop.Args);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
