using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Aeon.DiskImages;
using Aeon.Emulator.Configuration;
using Aeon.Emulator.Dos.VirtualFileSystem;

namespace Aeon.Emulator.Launcher;

public sealed partial class MainWindow : Window
{
    private PerformanceWindow? performanceWindow;
    private AeonConfiguration? currentConfig;
    private bool hasActivated;
    private PaletteDialog? paletteWindow;

    public MainWindow()
    {
        InitializeComponent();
        this.emulatorDisplay.EmulatorStateChanged += EmulatorDisplay_EmulatorStateChanged;
        this.emulatorDisplay.EmulationError += EmulatorDisplay_EmulationError;
        this.emulatorDisplay.CurrentProcessChanged += EmulatorDisplay_CurrentProcessChanged;
        this.speedLabel.Text = FormatSpeed(emulatorDisplay.EmulationSpeed);
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
        mouseIntegrationButton.IsChecked = emulatorDisplay.MouseInputMode == MouseInputMode.Absolute;
        speedLabel.Text = FormatSpeed(emulatorDisplay.EmulationSpeed);
        UpdateSpeedButtonStates();
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
        var taskDialog = new TaskDialog { Items = items, Title = title, Caption = caption, Icon = this.Icon };
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
            using var stream = new MemoryStream();
            bmp.Save(stream);
            stream.Position = 0;
            var bytes = stream.ToArray();

#pragma warning disable CS0618 // DataObject/SetDataObjectAsync are deprecated but replacement API is complex
            var dataObject = new DataObject();
            dataObject.Set("PNG", bytes);
            await this.Clipboard.SetDataObjectAsync(dataObject);
#pragma warning restore CS0618
        }
    }

    private void Pause_Click(object? sender, RoutedEventArgs e)
    {
        emulatorDisplay.PauseCommand.Execute(null);
    }

    private void Resume_Click(object? sender, RoutedEventArgs e)
    {
        emulatorDisplay.ResumeCommand.Execute(null);
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void FullScreen_Click(object? sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void MouseIntegration_Changed(object? sender, RoutedEventArgs e)
    {
        emulatorDisplay.MouseInputMode = mouseIntegrationButton.IsChecked == true
            ? MouseInputMode.Absolute
            : MouseInputMode.Relative;
    }

    private void AspectRatioCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        emulatorDisplay.IsAspectRatioLocked = aspectRatioCheckBox.IsChecked == true;
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
            this.Background = (IBrush?)this.FindResource("backgroundGradient") ?? Brushes.SteelBlue;
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
            speedLabel.Text = FormatSpeed(newSpeed);
        }
        UpdateSpeedButtonStates();
    }

    private void FasterButton_Click(object? sender, RoutedEventArgs e)
    {
        int newSpeed = emulatorDisplay.EmulationSpeed + 100_000;
        if (newSpeed != emulatorDisplay.EmulationSpeed)
        {
            emulatorDisplay.EmulationSpeed = newSpeed;
            speedLabel.Text = FormatSpeed(newSpeed);
        }
        UpdateSpeedButtonStates();
    }

    private void UpdateSpeedButtonStates()
    {
        slowerButton.IsEnabled = emulatorDisplay.EmulationSpeed > EmulatorHost.MinimumSpeed;
        // No maximum speed in the original WPF (only minimum speed of 2 via validation)
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
            this.paletteWindow = new PaletteDialog { EmulatorDisplay = this.emulatorDisplay, Icon = this.Icon };
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
