namespace Aeon.Emulator.Launcher;

using System;
using System.Windows.Input;

using Aeon.Emulator.Video;
using Aeon.Emulator.Video.Rendering;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Platform;
using global::Avalonia.Threading;

public partial class EmulatorDisplay : Window
{
    private static readonly StyledProperty<EmulatorState> EmulatorStateProperty = AvaloniaProperty.Register<EmulatorDisplay, EmulatorState>(nameof(EmulatorState), EmulatorState.NoProgram);
    private static readonly StyledProperty<bool> IsMouseCursorCapturedProperty = AvaloniaProperty.Register<EmulatorDisplay, bool>(nameof(IsMouseCursorCaptured), false);
    private static readonly StyledProperty<Dos.DosProcess?> CurrentProcessProperty = AvaloniaProperty.Register<EmulatorDisplay, Dos.DosProcess?>(nameof(CurrentProcess), null);

    public static readonly RoutedEvent EmulatorStateChangedEvent = RoutedEvent.Register<EmulatorDisplay, RoutedEventArgs>(nameof(EmulatorStateChanged), RoutingStrategies.Bubble);
    public static readonly RoutedEvent EmulationErrorEvent = RoutedEvent.Register<EmulatorDisplay, EmulationErrorRoutedEventArgs>(nameof(EmulationError), RoutingStrategies.Bubble);
    public static readonly RoutedEvent CurrentProcessChangedEvent = RoutedEvent.Register<EmulatorDisplay, RoutedEventArgs>(nameof(CurrentProcessChanged), RoutingStrategies.Bubble);
    public static readonly StyledProperty<bool> IsAspectRatioLockedProperty = AvaloniaProperty.Register<EmulatorDisplay, bool>(nameof(IsAspectRatioLocked), true, coerce: OnIsAspectRatioLockedChanged);
    public static readonly StyledProperty<MouseInputMode> MouseInputModeProperty = AvaloniaProperty.Register<EmulatorDisplay, MouseInputMode>(nameof(MouseInputMode), MouseInputMode.Relative);
    public static readonly StyledProperty<int> EmulationSpeedProperty = AvaloniaProperty.Register<EmulatorDisplay, int>(nameof(EmulationSpeed), 20_000_000, coerce: OnEmulationSpeedChanged, validate: EmulationSpeedChangedValidate);
    public static readonly StyledProperty<ScalingAlgorithm> ScalingAlgorithmProperty = AvaloniaProperty.Register<EmulatorDisplay, ScalingAlgorithm>(nameof(ScalingAlgorithm), ScalingAlgorithm.None, coerce: OnScalingAlgorithmChanged);

    private EmulatorHost emulator;
    private bool mouseJustCaptured;
    private bool isMouseCaptured;
    private Video.Point centerPoint;
    private DispatcherTimer timer;
    private readonly EventHandler updateHandler;
    private int cursorBlink;
    private Video.Point cursorPosition = new(0, 1);
    private readonly SimpleCommand resumeCommand;
    private readonly SimpleCommand pauseCommand;
    public static readonly RoutedCommand FullScreenCommand = new();
    private Presenter currentPresenter;
    private int physicalMemorySize = 16;
    private WriteableBitmap renderTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmulatorDisplay"/> class.
    /// </summary>
    public EmulatorDisplay()
    {
        updateHandler = new EventHandler(this.GraphicalUpdate);
        this.resumeCommand = new SimpleCommand(() => this.EmulatorState == EmulatorState.Paused, () => { this.EmulatorHost.Run(); });
        this.pauseCommand = new SimpleCommand(() => this.EmulatorState == EmulatorState.Running, () => { this.EmulatorHost.Pause(); });
        InitializeComponent();
        displayImage.PointerPressed += DisplayImage_MouseDown;
        displayImage.PointerReleased += DisplayImage_MouseUp;
        displayImage.PointerMoved += DisplayImage_MouseMove;
#if DEBUG
        this.AttachDevTools();
#endif
        updateHandler += this.GraphicalUpdate;
    }

    /// <summary>
    /// Occurs when the emulator's state has changed.
    /// </summary>
    public event EventHandler EmulatorStateChanged
    {
        add { this.AddHandler(EmulatorStateChangedEvent, value); }
        remove { this.RemoveHandler(EmulatorStateChangedEvent, value); }
    }
    /// <summary>
    /// Occurs when an error in emulation causes the emulator to halt.
    /// </summary>
    public event EmulationErrorRoutedEventHandler EmulationError
    {
        add { this.AddHandler(EmulationErrorEvent, value); }
        remove { this.RemoveHandler(EmulationErrorEvent, value); }
    }
    /// <summary>
    /// Occurs when the current process has changed.
    /// </summary>
    public event EventHandler CurrentProcessChanged
    {
        add { this.AddHandler(CurrentProcessChangedEvent, value); }
        remove { this.RemoveHandler(CurrentProcessChangedEvent, value); }
    }

    /// <summary>
    /// Gets or sets the emulator to display.
    /// </summary>
    public EmulatorHost EmulatorHost
    {
        get
        {
            if (this.emulator == null)
            {
                this.emulator = new EmulatorHost(this.physicalMemorySize) { EventSynchronizer = new AvaloniaSynchronizer(Dispatcher.UIThread) };
                this.emulator.VideoModeChanged += this.HandleModeChange;
                this.emulator.StateChanged += this.Emulator_StateChanged;
                this.emulator.MouseVisibilityChanged += this.Emulator_MouseVisibilityChanged;
                this.emulator.MouseMove += this.Emulator_MouseMove;
                this.emulator.Error += this.Emulator_Error;
                this.emulator.CurrentProcessChanged += this.Emulator_CurrentProcessChanged;
                this.emulator.EmulationSpeed = this.EmulationSpeed;
                this.timer.Start();
                this.InitializePresenter();
            }

            return this.emulator;
        }
    }
    /// <summary>
    /// Gets the current state of the emulator.  This is a dependency property.
    /// </summary>
    public EmulatorState EmulatorState => (EmulatorState)this.GetValue(EmulatorStateProperty);
    /// <summary>
    /// Gets or sets a value indicating whether the correct aspect ratio is maintained. This is a dependency property.
    /// </summary>
    public bool IsAspectRatioLocked
    {
        get => (bool)this.GetValue(IsAspectRatioLockedProperty);
        set => this.SetValue(IsAspectRatioLockedProperty, value);
    }
    /// <summary>
    /// Gets or sets a value indicating the type of mouse input provided.  This is a dependency property.
    /// </summary>
    public MouseInputMode MouseInputMode
    {
        get => (MouseInputMode)this.GetValue(MouseInputModeProperty);
        set => this.SetValue(MouseInputModeProperty, value);
    }
    /// <summary>
    /// Gets a value indicating whether the emulator has captured mouse input.  This is a dependency property.
    /// </summary>
    public bool IsMouseCursorCaptured => (bool)this.GetValue(IsMouseCursorCapturedProperty);
    /// <summary>
    /// Gets or sets the emulation speed.  This is a dependency property.
    /// </summary>
    public int EmulationSpeed
    {
        get => (int)this.GetValue(EmulationSpeedProperty);
        set => this.SetValue(EmulationSpeedProperty, value);
    }
    /// <summary>
    /// Gets or sets the scaling algorithm. This is a dependency property.
    /// </summary>
    public ScalingAlgorithm ScalingAlgorithm
    {
        get => (ScalingAlgorithm)this.GetValue(ScalingAlgorithmProperty);
        set => this.SetValue(ScalingAlgorithmProperty, value);
    }
    /// <summary>
    /// Gets the BitmapSource used for rendering the output display.
    /// </summary>
    public IImage DisplayBitmap => this.renderTarget;
    /// <summary>
    /// Gets information about the current process. This is a dependency property.
    /// </summary>
    public Emulator.Dos.DosProcess CurrentProcess => this.GetValue(CurrentProcessProperty);

    /// <summary>
    /// Gets the command used to resume the emulator from a paused state.
    /// </summary>
    public ICommand ResumeCommand => this.resumeCommand;
    /// <summary>
    /// Gets the command used to pause the emulator.
    /// </summary>
    public ICommand PauseCommand => this.pauseCommand;

    public Presenter CurrentPresenter => this.currentPresenter;

    /// <summary>
    /// Disposes the current emulator and returns the control to its default state.
    /// </summary>
    public void ResetEmulator(int physicalMemory = 16)
    {
        this.physicalMemorySize = physicalMemory;
        if (this.emulator != null)
        {
            this.emulator.VideoModeChanged -= this.HandleModeChange;
            this.emulator.StateChanged -= this.Emulator_StateChanged;
            this.emulator.MouseVisibilityChanged -= this.Emulator_MouseVisibilityChanged;
            this.emulator.MouseMove -= this.Emulator_MouseMove;
            this.emulator.Error -= this.Emulator_Error;
            this.emulator.CurrentProcessChanged -= this.Emulator_CurrentProcessChanged;
            this.mouseImage.IsVisible = false;
            this.cursorRectangle.IsVisible = false;
            this.timer.Stop();

            this.emulator.Dispose();
            this.emulator = null;
        }
    }


    protected override void OnInitialized()
    {
        this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / 60.0), DispatcherPriority.Render, updateHandler);
        base.OnInitialized();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.System && e.Key == Key.Enter &&
            e.KeyModifiers == KeyModifiers.Alt)
            FullScreenCommand.Execute(null, this);

        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (e.Key == Key.F12 && e.KeyModifiers == KeyModifiers.Control)
            {
                this.isMouseCaptured = false;
                this.SetValue(IsMouseCursorCapturedProperty, false);
            }
            else
            {
                var key = e.Key.ToEmulatorKey();
                if (key != Keys.Null)
                    emulator.PressKey(key);
            }

            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            var key = e.Key.ToEmulatorKey();
            if (key != Keys.Null)
                this.emulator.ReleaseKey(key);

            e.Handled = true;
        }

        base.OnKeyUp(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        this.isMouseCaptured = false;
        this.SetValue(IsMouseCursorCapturedProperty, false);
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
            this.emulator.ReleaseAllKeys();

        base.OnLostFocus(e);
    }

    private void GraphicalUpdate(object sender, EventArgs e)
    {
        if (this.emulator != null)
        {
            var presenter = this.currentPresenter;
            if (presenter == null)
                return;

            this.EnsureRenderTarget(presenter);

            presenter.Update(this.renderTarget.Lock().Address);
            this.displayImage.InvalidateVisual();

            if (this.emulator.VirtualMachine.IsCursorVisible)
            {
                this.cursorBlink = (this.cursorBlink + 1) % 16;
                if (this.cursorBlink == 8)
                    this.cursorRectangle.IsVisible = true;
                else if (cursorBlink == 0)
                    this.cursorRectangle.IsVisible = false;

                var cursorPosition = this.emulator.VirtualMachine.CursorPosition;
                if (cursorPosition != this.cursorPosition)
                {
                    this.cursorPosition = cursorPosition;
                    Canvas.SetLeft(this.cursorRectangle, cursorPosition.X * 8);
                    Canvas.SetTop(this.cursorRectangle, (cursorPosition.Y * emulator.VirtualMachine.VideoMode.FontHeight) + emulator.VirtualMachine.VideoMode.FontHeight - 2);
                }
            }
            else if (this.cursorRectangle.IsVisible)
            {
                this.cursorRectangle.IsVisible = false;
            }
        }
    }
    private void HandleModeChange(object sender, EventArgs e) => this.InitializePresenter();
    private void InitializePresenter()
    {
        this.displayImage.Source = null;
        var oldPresenter = this.currentPresenter;
        this.currentPresenter = null;
        oldPresenter?.Dispose();

        if (this.emulator == null)
            return;

        var videoMode = this.emulator.VirtualMachine.VideoMode;
        this.currentPresenter = this.GetPresenter(videoMode);
        this.currentPresenter.Scaler = this.ScalingAlgorithm;
        this.EnsureRenderTarget(this.currentPresenter);

        int pixelWidth = this.currentPresenter.TargetWidth;
        int pixelHeight = this.currentPresenter.TargetHeight;
        this.displayImage.Source = this.renderTarget;
        this.displayImage.Width = pixelWidth;
        this.displayImage.Height = pixelHeight;
        this.displayArea.Width = pixelWidth;
        this.displayArea.Height = pixelHeight;

        this.centerPoint.X = pixelWidth / 2;
        this.centerPoint.Y = pixelHeight / 2;
    }
    private void EnsureRenderTarget(Presenter presenter)
    {
        if (this.renderTarget == null || presenter.TargetWidth != this.renderTarget.PixelSize.Width || presenter.TargetHeight != this.renderTarget.PixelSize.Height)
        {
            this.renderTarget?.Dispose();
            this.renderTarget = new
                (new(presenter.TargetWidth,
                presenter.TargetHeight),
                new(presenter.TargetWidth, presenter.TargetHeight),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);
        }
    }
    private void MoveMouseCursor(int x, int y)
    {
        var presenter = this.currentPresenter;
        if (presenter != null)
        {
            Canvas.SetLeft(mouseImage, x * presenter.WidthRatio);
            Canvas.SetTop(mouseImage, y * presenter.HeightRatio);
        }
    }
    private Presenter? GetPresenter(VideoMode videoMode)
    {
        if (this.emulator == null)
            return null;

        if (videoMode.VideoModeType == VideoModeType.Text)
        {
            return new TextPresenter(videoMode);
        }
        else
        {
            return videoMode.BitsPerPixel switch
            {
                2 => new GraphicsPresenter2(videoMode),
                4 => new GraphicsPresenter4(videoMode),
                8 when videoMode.IsPlanar => new GraphicsPresenterX(videoMode),
                8 when !videoMode.IsPlanar => new GraphicsPresenter8(videoMode),
                16 => new GraphicsPresenter16(videoMode),
                _ => null
            };
        }
    }

    private static int OnEmulationSpeedChanged(IAvaloniaObject d, int e)
    {
        var obj = (EmulatorDisplay)d;
        if (obj.emulator != null)
            obj.emulator.EmulationSpeed = e;
        return e;
    }
    private static bool EmulationSpeedChangedValidate(int value)
    {
        int n = (int)value;
        return n >= EmulatorHost.MinimumSpeed;
    }

    private static bool OnIsAspectRatioLockedChanged(IAvaloniaObject d, bool e)
    {
        var obj = (EmulatorDisplay)d;
        bool value = e;
        obj.outerViewbox.Stretch = value ? Stretch.Uniform : Stretch.Fill;
        return e;
    }

    private static ScalingAlgorithm OnScalingAlgorithmChanged(IAvaloniaObject d, ScalingAlgorithm e)
    {
        var obj = (EmulatorDisplay)d;
        obj.InitializePresenter();
        return e;
    }

    private void Emulator_StateChanged(object sender, EventArgs e)
    {
        if (this.emulator != null)
        {
            this.SetValue(EmulatorStateProperty, this.emulator.State);
            this.RaiseEvent(new RoutedEventArgs(EmulatorStateChangedEvent));
        }
    }
    private void Emulator_MouseVisibilityChanged(object sender, EventArgs e)
    {
        this.mouseImage.IsVisible = this.emulator.VirtualMachine.IsMouseVisible;
    }
    private void Emulator_MouseMove(object sender, MouseMoveEventArgs e) => this.MoveMouseCursor(e.X, e.Y);
    private void Emulator_Error(object sender, ErrorEventArgs e) => this.RaiseEvent(new EmulationErrorRoutedEventArgs(EmulationErrorEvent, e.Message));
    private void Emulator_CurrentProcessChanged(object sender, EventArgs e)
    {
        if (this.emulator != null)
            this.SetValue(CurrentProcessProperty, this.emulator.VirtualMachine.CurrentProcess);
        else
            this.SetValue(CurrentProcessProperty, null);

        this.RaiseEvent(new RoutedEventArgs(CurrentProcessChangedEvent));
    }
    private void DisplayImage_MouseDown(object? sender, PointerPressedEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (!this.isMouseCaptured && this.MouseInputMode == MouseInputMode.Relative)
            {
                this.SetValue(IsMouseCursorCapturedProperty, true);
                this.mouseJustCaptured = true;
                this.isMouseCaptured = true;

                this.centerPoint.X = (int)(displayImage.Width / 2);
                this.centerPoint.Y = (int)(displayImage.Height / 2);
                return;
            }

            var button = e.MouseButton.ToEmulatorButtons();
            if (button != MouseButtons.None)
            {
                var mouseEvent = new MouseButtonDownEvent(button);
                this.emulator.MouseEvent(mouseEvent);
            }
        }
    }
    private void DisplayImage_MouseUp(object? sender, PointerReleasedEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (this.mouseJustCaptured)
            {
                this.mouseJustCaptured = false;
                return;
            }

            var button = e.MouseButton.ToEmulatorButtons();
            if (button != MouseButtons.None)
            {
                var mouseEvent = new MouseButtonUpEvent(button);
                this.emulator.MouseEvent(mouseEvent);
            }
        }
    }
    private void DisplayImage_MouseMove(object? sender, PointerEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            var presenter = this.currentPresenter;

            if (this.MouseInputMode == MouseInputMode.Absolute)
            {
                var pos = e.GetPosition(displayImage);
                this.emulator.MouseEvent(new MouseMoveAbsoluteEvent((int)(pos.X / presenter.WidthRatio), (int)(pos.Y / presenter.HeightRatio)));
            }
            else if (this.isMouseCaptured)
            {
                var deltaPos = e.GetCurrentPoint(this.displayImage);

                int dx = (int)(deltaPos.Position.X - this.centerPoint.X) / presenter.WidthRatio;
                int dy = (int)(deltaPos.Position.Y - this.centerPoint.Y) / presenter.HeightRatio;

                if (dx != 0 || dy != 0)
                {
                    this.emulator.MouseEvent(new MouseMoveRelativeEvent(dx, dy));
                    var p = this.displayImage.PointToScreen(
                        new(centerPoint.X, centerPoint.Y));
                    //_ = NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                }
            }
        }
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public delegate void EmulationErrorRoutedEventHandler(object sender, EmulationErrorRoutedEventArgs e);
}
