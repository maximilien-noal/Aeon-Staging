using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Aeon.DiskImages;
using Aeon.Emulator.Configuration;
using Aeon.Emulator.Dos.VirtualFileSystem;

namespace Aeon.Emulator.Launcher;

public sealed class MainWindow : Window
{
    private PerformanceWindow? performanceWindow;
    private AeonConfiguration? currentConfig;
    private bool hasActivated;
    private PaletteDialog? paletteWindow;
    private readonly EmulatorDisplay emulatorDisplay;
    private readonly StackPanel menuContainer;

    public MainWindow()
    {
        this.Title = "Aeon";
        this.Width = 800;
        this.Height = 600;
        this.MinWidth = 360;
        this.MinHeight = 270;
        this.Background = new LinearGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Colors.SteelBlue, 0),
                new GradientStop(Color.Parse("#FF1D4461"), 1)
            }
        };

        this.emulatorDisplay = new EmulatorDisplay { Margin = new Thickness(0, 7, 0, 0) };
        this.emulatorDisplay.EmulatorStateChanged += EmulatorDisplay_EmulatorStateChanged;
        this.emulatorDisplay.EmulationError += EmulatorDisplay_EmulationError;
        this.emulatorDisplay.CurrentProcessChanged += EmulatorDisplay_CurrentProcessChanged;

        var menuBar = BuildMenu();
        var toolBar = BuildToolBar();

        this.menuContainer = new StackPanel();
        this.menuContainer.Children.Add(menuBar);
        this.menuContainer.Children.Add(toolBar);

        var dockPanel = new DockPanel();
        DockPanel.SetDock(this.menuContainer, Dock.Top);
        dockPanel.Children.Add(this.menuContainer);
        dockPanel.Children.Add(this.emulatorDisplay);

        this.Content = dockPanel;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (!this.hasActivated)
        {
            var args = App.Args;

            if (args.Length > 0)
                QuickLaunch(args[0]);

            this.hasActivated = true;
        }

        this.emulatorDisplay.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            ToggleFullScreen();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private Menu BuildMenu()
    {
        var quickLaunchItem = new MenuItem { Header = "_Quick Launch Program..." };
        quickLaunchItem.Click += QuickLaunch_Click;

        var commandPromptItem = new MenuItem { Header = "_Quick Launch Command Prompt..." };
        commandPromptItem.Click += CommandPrompt_Click;

        var pauseItem = new MenuItem { Header = "_Pause" };
        pauseItem.Click += (_, _) => emulatorDisplay.PauseCommand.Execute(null);

        var resumeItem = new MenuItem { Header = "R_esume" };
        resumeItem.Click += (_, _) => emulatorDisplay.ResumeCommand.Execute(null);

        var exitItem = new MenuItem { Header = "E_xit" };
        exitItem.Click += (_, _) => this.Close();

        var aeonMenu = new MenuItem
        {
            Header = "_Aeon",
            Items = { quickLaunchItem, commandPromptItem, new Separator(), pauseItem, resumeItem, new Separator(), exitItem }
        };

        var copyItem = new MenuItem { Header = "_Copy Screen" };
        copyItem.Click += Copy_Click;
        var editMenu = new MenuItem { Header = "_Edit", Items = { copyItem } };

        var fullScreenItem = new MenuItem { Header = "_Full Screen" };
        fullScreenItem.Click += (_, _) => ToggleFullScreen();

        var perfItem = new MenuItem { Header = "_Performance Window" };
        perfItem.Click += PerformanceWindow_Click;

        var viewMenu = new MenuItem
        {
            Header = "_View",
            Items = { fullScreenItem, new Separator(), perfItem }
        };

        var paletteItem = new MenuItem { Header = "Color Palette" };
        paletteItem.Click += ShowPalette_Click;
        var debugMenu = new MenuItem { Header = "_Debug", Items = { paletteItem } };

        return new Menu { Items = { aeonMenu, editMenu, viewMenu, debugMenu } };
    }

    private StackPanel BuildToolBar()
    {
        var openButton = new Button { Content = "📂" };
        ToolTip.SetTip(openButton, "Run Program...");
        openButton.Click += QuickLaunch_Click;

        var resumeButton = new Button { Content = "▶" };
        ToolTip.SetTip(resumeButton, "Resume");
        resumeButton.Click += (_, _) => emulatorDisplay.ResumeCommand.Execute(null);

        var pauseButton = new Button { Content = "⏸" };
        ToolTip.SetTip(pauseButton, "Pause");
        pauseButton.Click += (_, _) => emulatorDisplay.PauseCommand.Execute(null);

        var slowerButton = new Button { Content = "-" };
        ToolTip.SetTip(slowerButton, "Slow Down Emulation");
        slowerButton.Click += SlowerButton_Click;

        var speedLabel = new TextBlock
        {
            Width = 80,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = FormatSpeed(emulatorDisplay.EmulationSpeed)
        };
        this.speedLabel = speedLabel;

        var fasterButton = new Button { Content = "+" };
        ToolTip.SetTip(fasterButton, "Speed Up Emulation");
        fasterButton.Click += FasterButton_Click;

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(4),
            Children =
            {
                openButton,
                new Panel { Width = 1, Background = Brushes.Gray, Margin = new Thickness(4, 0) },
                resumeButton,
                pauseButton,
                new Panel { Width = 1, Background = Brushes.Gray, Margin = new Thickness(4, 0) },
                new TextBlock { Text = "Speed:", VerticalAlignment = VerticalAlignment.Center },
                slowerButton,
                speedLabel,
                fasterButton
            }
        };
    }

    private TextBlock? speedLabel;

    private static string FormatSpeed(int speed)
    {
        var mhz = (decimal)speed / 1_000_000;
        return mhz.ToString("0.#") + "MHz";
    }

    private void ApplyConfiguration(AeonConfiguration config)
    {
        this.emulatorDisplay.EmulatorHost = new EmulatorHost(
            new VirtualMachineInitializationOptions
            {
                AdditionalDevices =
                [
                    _ => new Sound.PCSpeaker.InternalSpeaker(),
                    vm => new Sound.Blaster.SoundBlaster(vm),
                    _ => new Sound.FM.FmSoundCard(),
                    _ => new Sound.GeneralMidi(new Sound.GeneralMidiOptions(config.MidiEngine ?? Sound.MidiEngine.MidiMapper, config.SoundfontPath, config.Mt32RomsPath))
                ]
            }
        )
        {
            EventSynchronizer = new AvaloniaSynchronizer()
        };

        if (config.Drives != null)
        {
            foreach (var (letter, info) in config.Drives)
        {
            var driveLetter = ParseDriveLetter(letter);

            var vmDrive = this.emulatorDisplay.EmulatorHost!.VirtualMachine.FileSystem.Drives[driveLetter];
            vmDrive.DriveType = info.Type;
            vmDrive.VolumeLabel = info.Label;
            if (info.FreeSpace != null)
                vmDrive.FreeSpace = info.FreeSpace.GetValueOrDefault();

            if (!string.IsNullOrEmpty(info.HostPath))
            {
                vmDrive.Mapping = info.ReadOnly ? new MappedFolder(info.HostPath) : new WritableMappedFolder(info.HostPath);
            }
            else if (!string.IsNullOrEmpty(info.ImagePath))
            {
                if (Path.GetExtension(info.ImagePath).Equals(".iso", StringComparison.OrdinalIgnoreCase))
                    vmDrive.Mapping = new ISOImage(info.ImagePath);
                else if (Path.GetExtension(info.ImagePath).Equals(".cue", StringComparison.OrdinalIgnoreCase))
                    vmDrive.Mapping = new CueSheetImage(info.ImagePath);
                else
                    throw new FormatException();
            }
            else
            {
                throw new FormatException();
            }

            vmDrive.HasCommandInterpreter = vmDrive.DriveType == DriveType.Fixed;
        }
        }

        this.emulatorDisplay.EmulatorHost!.VirtualMachine.FileSystem.WorkingDirectory = new VirtualPath(config.StartupPath ?? string.Empty);

        emulatorDisplay.EmulationSpeed = config.EmulationSpeed ?? 100_000_000;
        emulatorDisplay.MouseInputMode = config.IsMouseAbsolute.GetValueOrDefault() ? MouseInputMode.Absolute : MouseInputMode.Relative;
        if (!string.IsNullOrEmpty(config.Title))
            this.Title = config.Title;

        static DriveLetter ParseDriveLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));
            if (s.Length != 1)
                throw new FormatException();

            return new DriveLetter(s[0]);
        }
    }

    private void LaunchCurrentConfig()
    {
        if (this.currentConfig == null)
            return;

        ApplyConfiguration(this.currentConfig);
        if (!string.IsNullOrEmpty(this.currentConfig.Launch))
        {
            var launchTargets = this.currentConfig.Launch.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (launchTargets.Length == 1)
                this.emulatorDisplay.EmulatorHost!.LoadProgram(launchTargets[0]);
            else
                this.emulatorDisplay.EmulatorHost!.LoadProgram(launchTargets[0], launchTargets[1]);
        }
        else
        {
            this.emulatorDisplay.EmulatorHost!.LoadProgram("COMMAND.COM");
        }

        this.emulatorDisplay.EmulatorHost.Run();
    }

    private void QuickLaunch(string fileName)
    {
        bool hasConfig = fileName.EndsWith(".AeonConfig", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".AeonPack", StringComparison.OrdinalIgnoreCase);
        if (hasConfig)
            this.currentConfig = AeonConfiguration.Load(fileName);
        else
            this.currentConfig = AeonConfiguration.GetQuickLaunchConfiguration(Path.GetDirectoryName(fileName)!, Path.GetFileName(fileName));

        this.LaunchCurrentConfig();
    }

    private async Task<TaskDialogItem?> ShowTaskDialog(string title, string caption, params TaskDialogItem[] items)
    {
        var taskDialog = new TaskDialog { Items = items, Title = title, Caption = caption };
        var result = await taskDialog.ShowDialog<bool?>(this);
        if (result == true)
            return taskDialog.SelectedItem;
        return null;
    }

    private async void QuickLaunch_Click(object? sender, RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Run DOS program...",
            FileTypeFilter =
            [
                new FilePickerFileType("Programs") { Patterns = ["*.exe", "*.com", "*.AeonConfig", "*.AeonPack"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path != null)
                this.QuickLaunch(path);
        }
    }

    private async void CommandPrompt_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder for C:\\ drive..."
        });

        if (folders.Count > 0)
        {
            var path = folders[0].TryGetLocalPath();
            if (path != null)
            {
                this.currentConfig = AeonConfiguration.GetQuickLaunchConfiguration(path, string.Empty);
                this.LaunchCurrentConfig();
            }
        }
    }

    private async void Copy_Click(object? sender, RoutedEventArgs e)
    {
        var bmp = emulatorDisplay.DisplayBitmap;
        if (bmp != null && this.Clipboard != null)
        {
            // Avalonia clipboard doesn't directly support images in the same way as WPF.
            // For now we just provide a text notification. Full clipboard image support
            // would need platform-specific code or a future Avalonia API.
            await this.Clipboard.SetTextAsync("[Screen copied - image clipboard not yet implemented]");
        }
    }

    private void ToggleFullScreen()
    {
        if (this.WindowState != WindowState.FullScreen)
        {
            this.menuContainer.IsVisible = false;
            this.WindowState = WindowState.FullScreen;
            this.Background = Brushes.Black;
        }
        else
        {
            this.menuContainer.IsVisible = true;
            this.WindowState = WindowState.Normal;
            this.Background = new LinearGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Colors.SteelBlue, 0),
                    new GradientStop(Color.Parse("#FF1D4461"), 1)
                }
            };
        }
    }

    private void EmulatorDisplay_EmulatorStateChanged(object? sender, RoutedEventArgs e)
    {
        if (this.emulatorDisplay.EmulatorState == EmulatorState.ProgramExited && this.currentConfig != null)
            this.Close();
    }

    private void SlowerButton_Click(object? sender, RoutedEventArgs e)
    {
        int newSpeed = Math.Max(EmulatorHost.MinimumSpeed, emulatorDisplay.EmulationSpeed - 100_000);
        if (newSpeed != emulatorDisplay.EmulationSpeed)
        {
            emulatorDisplay.EmulationSpeed = newSpeed;
            if (speedLabel != null)
                speedLabel.Text = FormatSpeed(newSpeed);
        }
    }

    private void FasterButton_Click(object? sender, RoutedEventArgs e)
    {
        int newSpeed = emulatorDisplay.EmulationSpeed + 100_000;
        if (newSpeed != emulatorDisplay.EmulationSpeed)
        {
            emulatorDisplay.EmulationSpeed = newSpeed;
            if (speedLabel != null)
                speedLabel.Text = FormatSpeed(newSpeed);
        }
    }

    private async void EmulatorDisplay_EmulationError(object? sender, EmulationErrorRoutedEventArgs e)
    {
        var end = new TaskDialogItem("End Program", "Terminates the current emulation session.");
        await ShowTaskDialog("Emulation Error", "An error occurred which caused the emulator to halt: " + e.Message + " What would you like to do?", end);
    }

    private void EmulatorDisplay_CurrentProcessChanged(object? sender, RoutedEventArgs e)
    {
        if (this.currentConfig == null || string.IsNullOrEmpty(this.currentConfig.Title))
        {
            var process = emulatorDisplay.CurrentProcess;
            if (process != null)
                this.Title = $"{process} - Aeon";
            else
                this.Title = "Aeon";
        }
    }

    private void PerformanceWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (performanceWindow != null)
            performanceWindow.Activate();
        else
        {
            performanceWindow = new PerformanceWindow();
            performanceWindow.Closed += this.PerformanceWindow_Closed;
            performanceWindow.EmulatorDisplay = emulatorDisplay;
            performanceWindow.Show(this);
        }
    }

    private void PerformanceWindow_Closed(object? sender, EventArgs e)
    {
        if (performanceWindow != null)
        {
            performanceWindow.Closed -= this.PerformanceWindow_Closed;
            performanceWindow = null;
        }
    }

    private void ShowPalette_Click(object? sender, RoutedEventArgs e)
    {
        if (this.paletteWindow != null)
        {
            this.paletteWindow.Activate();
        }
        else
        {
            this.paletteWindow = new PaletteDialog { EmulatorDisplay = this.emulatorDisplay };
            this.paletteWindow.Closed += PaletteWindow_Closed;
            this.paletteWindow.Show(this);
        }
    }

    private void PaletteWindow_Closed(object? sender, EventArgs e)
    {
        if (this.paletteWindow != null)
        {
            this.paletteWindow.Closed -= this.PaletteWindow_Closed;
            this.paletteWindow = null;
        }
    }
}
