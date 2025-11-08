using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Aeon.Emulator.Launcher
{
    /// <summary>
    /// The Aeon application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Gets the current application instance.
        /// </summary>
        public static App Current => (App)Application.Current!;

        /// <summary>
        /// Gets the application command line arguments.
        /// </summary>
        public ReadOnlyCollection<string> Args { get; private set; } = new ReadOnlyCollection<string>(new List<string>());

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                this.Args = new ReadOnlyCollection<string>(desktop.Args?.ToList() ?? new List<string>());
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
