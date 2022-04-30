using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aeon.Avalonia;
using Aeon.DiskImages;
using Aeon.DiskImages.Archives;
using Aeon.Emulator.Dos.VirtualFileSystem;
using Aeon.Emulator.Input;
using Aeon.Emulator.Launcher.Configuration;
using Aeon.Emulator.Sound;
using Aeon.Emulator.Sound.Blaster;
using Aeon.Emulator.Sound.FM;
using Aeon.Emulator.Video.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace Aeon.Emulator.Launcher.Views;

public partial class MainWindow : Window
{
    private PerformanceWindow performanceWindow;
    private AeonConfiguration currentConfig;
    private bool hasActivated;
    private PaletteDialog paletteWindow;

    public Array ScalerValues  => Enum.GetValues(typeof(ScalingAlgorithm));

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (!this.hasActivated)
        {
            var args = App.Args;

            if (args.Count > 0)
                QuickLaunch(args[0]);

            this.hasActivated = true;
        }

        if (emulatorDisplay != null && this.WindowState != WindowState.Minimized)
            emulatorDisplay.Focus();
    }

    private void ApplyConfiguration(AeonConfiguration config)
    {
        this.emulatorDisplay.ResetEmulator(config.PhysicalMemorySize ?? 16);

        var globalConfig = GlobalConfiguration.Load();

        foreach (var (letter, info) in config.Drives)
        {
            var driveLetter = ParseDriveLetter(letter);

            var vmDrive = this.emulatorDisplay.EmulatorHost.VirtualMachine.FileSystem.Drives[driveLetter];
            vmDrive.DriveType = info.Type;
            vmDrive.VolumeLabel = info.Label;
            if (info.FreeSpace != null)
                vmDrive.FreeSpace = info.FreeSpace.GetValueOrDefault();

            if (config.Archive == null)
            {
                if (!string.IsNullOrEmpty(info.HostPath))
                {
                    vmDrive.Mapping = info.ReadOnly
                        ? new MappedFolder(info.HostPath)
                        : new WritableMappedFolder(info.HostPath);
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
            }
            else
            {
                if (string.IsNullOrEmpty(info.ImagePath))
                {
                    if (info.ReadOnly)
                    {
                        vmDrive.Mapping = new MappedArchive(driveLetter, config.Archive);
                    }
                    else
                    {
                        var rootPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "Aeon Emulator", "Files", config.Id, letter.ToUpperInvariant());
                        vmDrive.Mapping = new DifferencingFolder(driveLetter, config.Archive, rootPath);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            vmDrive.HasCommandInterpreter = vmDrive.DriveType == DriveType.Fixed;
        }

        this.emulatorDisplay.EmulatorHost.VirtualMachine.FileSystem.WorkingDirectory =
            new VirtualPath(config.StartupPath);

        var vm = this.emulatorDisplay.EmulatorHost.VirtualMachine;
       
        vm.RegisterVirtualDevice(new Aeon.Emulator.Sound.PCSpeaker.InternalSpeaker());
        vm.RegisterVirtualDevice(new SoundBlaster(vm));
        vm.RegisterVirtualDevice(new FmSoundCard());
        vm.RegisterVirtualDevice(new GeneralMidi(globalConfig.Mt32Enabled ? globalConfig.Mt32RomsPath : null));

        vm.RegisterVirtualDevice(new JoystickDevice());

        emulatorDisplay.EmulationSpeed = config.EmulationSpeed ?? 20_000_000;
        emulatorDisplay.MouseInputMode = config.IsMouseAbsolute ? MouseInputMode.Absolute : MouseInputMode.Relative;
        toolBar.IsVisible = !config.HideUserInterface;
        menuContainer.IsVisible = !config.HideUserInterface;
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
        ApplyConfiguration(this.currentConfig);
        if (!string.IsNullOrEmpty(this.currentConfig.Launch))
        {
            var launchTargets =
                this.currentConfig.Launch.Split(new char[] {' ', '\t'}, 2, StringSplitOptions.RemoveEmptyEntries);
            if (launchTargets.Length == 1)
                this.emulatorDisplay.EmulatorHost.LoadProgram(launchTargets[0]);
            else
                this.emulatorDisplay.EmulatorHost.LoadProgram(launchTargets[0], launchTargets[1]);
        }
        else
        {
            this.emulatorDisplay.EmulatorHost.LoadProgram("COMMAND.COM");
        }

        this.emulatorDisplay.EmulatorHost.Run();
    }

    private void QuickLaunch(string fileName)
    {
        bool hasConfig = fileName.EndsWith(".AeonConfig", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".AeonPack", StringComparison.OrdinalIgnoreCase);
        if (hasConfig)
            this.currentConfig = AeonConfiguration.Load(fileName);
        else
            this.currentConfig =
                AeonConfiguration.GetQuickLaunchConfiguration(Path.GetDirectoryName(fileName),
                    Path.GetFileName(fileName));

        this.LaunchCurrentConfig();
    }

    private async Task<TaskDialogItem?> ShowTaskDialog(string title, string caption, params TaskDialogItem[] items)
    {
        var taskDialog = new TaskDialog(this){ Items = items, Icon = this.Icon, Title = title, Caption = caption};
        await taskDialog.ShowDialog(this);
        return taskDialog.SelectedItem;
    }

    private async void QuickLaunch_Click(object sender, RoutedEventArgs e)
    {
        var fileDialog = new OpenFileDialog()
        {
            Title = "Run DOS program... (*.AeonPack, *.AeonConfig, *.exe, or *.com)"
        };

        var files = await fileDialog.ShowAsync(this);
        if (files is not null && files.Any())
        {
            this.QuickLaunch(files.First());            
        }
    }

    private async void CommandPrompt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select folder for C:\\ drive..."
        };

        var dir = await dialog.ShowAsync(this);
        if(!string.IsNullOrWhiteSpace((dir)))
        {
            this.currentConfig = AeonConfiguration.GetQuickLaunchConfiguration(dir, null);
            this.LaunchCurrentConfig();
        }
    }

    [ICommand]
    public void CloseMethod() => this.Close();

    private void MapDrives_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (this.emulatorDisplay != null)
        {
            var state = this.emulatorDisplay.EmulatorState;
            e.CanExecute = state == EmulatorState.Running || state == EmulatorState.Paused;
        }
    }

    private void EmulatorDisplay_EmulatorStateChanged(object? sender, EventArgs eventArgs)
    {
        if (this.emulatorDisplay.EmulatorState == EmulatorState.ProgramExited && this.currentConfig != null &&
            this.currentConfig.HideUserInterface)
            this.Close();
    }

    private void SlowerButton_Click(object sender, RoutedEventArgs e)
    {
        if (emulatorDisplay != null)
        {
            int newSpeed = Math.Max(EmulatorHost.MinimumSpeed, emulatorDisplay.EmulationSpeed - 100_000);
            if (newSpeed != emulatorDisplay.EmulationSpeed)
                emulatorDisplay.EmulationSpeed = newSpeed;
        }
    }

    private void FasterButton_Click(object sender, RoutedEventArgs e)
    {
        if (emulatorDisplay != null)
        {
            int newSpeed = emulatorDisplay.EmulationSpeed + 100_000;
            if (newSpeed != emulatorDisplay.EmulationSpeed)
                emulatorDisplay.EmulationSpeed = newSpeed;
        }
    }

    [ICommand]
    private async void Copy()
    {
        var bmp = emulatorDisplay?.DisplayBitmap;
        if (bmp != null)
        {
            var data = new DataObject();
            data.Set(nameof(bmp), bmp);
            await Application.Current?.Clipboard?.SetDataObjectAsync(data);
        }
    }

    private void FullScreen_Executed(object? sender, ExecutedRoutedEventArgs e)
    {
        if (this.WindowState != WindowState.FullScreen)
        {
            this.menuContainer.IsVisible = false;
            this.WindowState = WindowState.FullScreen;
            this.SetValue(BackgroundProperty, Brushes.Black);
        }
        else
        {
            this.menuContainer.IsVisible = true;
            this.WindowState = WindowState.Normal;
            this.SetValue(BackgroundProperty, this.FindResource("backgroundGradient"));
        }
    }

    private async void EmulatorDisplay_EmulationError(object sender, EmulationErrorRoutedEventArgs e)
    {
        var end = new TaskDialogItem("End Program", "Terminates the current emulation session.");
        var debug = new TaskDialogItem("Debug", "View the current emulation session in the Aeon debugger.");

        var selection = await ShowTaskDialog("Emulation Error",
            $"An error occurred which caused the emulator to halt: {e.Message} What would you like to do?", end,
            debug);

        if (selection == end || selection == null)
        {
            emulatorDisplay.ResetEmulator();
        }
        else if (selection == debug)
        {
            var debuggerWindow = new DebuggerWindow(this)
            {
                EmulatorHost = this.emulatorDisplay.EmulatorHost
            };
            debuggerWindow.Show();
            debuggerWindow.UpdateDebugger();
        }
    }

    private void EmulatorDisplay_CurrentProcessChanged(object? sender, EventArgs eventArgs)
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

    private void PerformanceWindow_Click(object sender, RoutedEventArgs e)
    {
        if (performanceWindow != null)
            performanceWindow.Activate();
        else
        {
            performanceWindow = new PerformanceWindow(this);
            performanceWindow.Closed += this.PerformanceWindow_Closed;
            performanceWindow.EmulatorDisplay = emulatorDisplay;
            performanceWindow.Show();
        }
    }

    private void PerformanceWindow_Closed(object sender, EventArgs e)
    {
        if (performanceWindow != null)
        {
            performanceWindow.Closed -= this.PerformanceWindow_Closed;
            performanceWindow = null;
        }
    }

    private void ShowDebugger_Click(object sender, RoutedEventArgs e)
    {
        var debuggerWindow = new DebuggerWindow(this)
        {
            EmulatorHost = this.emulatorDisplay.EmulatorHost
        };
        debuggerWindow.Show();
        debuggerWindow.UpdateDebugger();
    }

    private void ShowPalette_Click(object sender, RoutedEventArgs e)
    {
        if (this.paletteWindow != null)
        {
            this.paletteWindow.Activate();
        }
        else
        {
            this.paletteWindow = new PaletteDialog(this)
            {
                EmulatorDisplay = this.emulatorDisplay, Icon = this.Icon
            };
            this.paletteWindow.Closed += PaletteWindow_Closed;
            paletteWindow.Show();
        }
    }

    private async void OpenInstructionLog_Click(object sender, RoutedEventArgs e)
    {
        var openFile = new OpenFileDialog
        {
            Title = "Open Log File.. (*.AeonLog)"
        };

        var files = await openFile.ShowAsync(this);
        if(files is not null && files.Any())
        {
            var log = LogAccessor.Open(files.First());
            InstructionLogWindow.ShowDialog(log);
        }
    }

    private void PaletteWindow_Closed(object sender, EventArgs e)
    {
        if (this.paletteWindow != null)
        {
            this.paletteWindow.Closed -= this.PaletteWindow_Closed;
            this.paletteWindow = null;
        }
    }

    private async void DumpVideoRam_Click(object sender, RoutedEventArgs e)
    {
        using var bmp = this.emulatorDisplay?.CurrentPresenter?.Dump();
        if (bmp != null)
        {
            var data = new DataObject();
            data.Set(nameof(bmp), bmp);
            await Application.Current?.Clipboard?.SetDataObjectAsync(data);
        }
    }

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}