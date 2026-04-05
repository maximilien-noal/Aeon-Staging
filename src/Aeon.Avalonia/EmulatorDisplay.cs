using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Aeon.Emulator.Video.Rendering;

namespace Aeon.Emulator.Launcher;

public sealed partial class EmulatorDisplay : ContentControl
{
    public static readonly StyledProperty<EmulatorState> EmulatorStateProperty =
        AvaloniaProperty.Register<EmulatorDisplay, EmulatorState>(nameof(EmulatorState), EmulatorState.NoProgram);
    public static readonly StyledProperty<MouseInputMode> MouseInputModeProperty =
        AvaloniaProperty.Register<EmulatorDisplay, MouseInputMode>(nameof(MouseInputMode), MouseInputMode.Relative);
    public static readonly StyledProperty<bool> IsMouseCursorCapturedProperty =
        AvaloniaProperty.Register<EmulatorDisplay, bool>(nameof(IsMouseCursorCaptured), false);
    public static readonly StyledProperty<int> EmulationSpeedProperty =
        AvaloniaProperty.Register<EmulatorDisplay, int>(nameof(EmulationSpeed), 20_000_000);
    public static readonly StyledProperty<bool> IsAspectRatioLockedProperty =
        AvaloniaProperty.Register<EmulatorDisplay, bool>(nameof(IsAspectRatioLocked), true);
    public static readonly StyledProperty<ScalingAlgorithm> ScalingAlgorithmProperty =
        AvaloniaProperty.Register<EmulatorDisplay, ScalingAlgorithm>(nameof(ScalingAlgorithm), ScalingAlgorithm.None);

    public static readonly RoutedEvent<RoutedEventArgs> EmulatorStateChangedEvent =
        RoutedEvent.Register<EmulatorDisplay, RoutedEventArgs>(nameof(EmulatorStateChanged), Avalonia.Interactivity.RoutingStrategies.Bubble);
    public static readonly RoutedEvent<EmulationErrorRoutedEventArgs> EmulationErrorEvent =
        RoutedEvent.Register<EmulatorDisplay, EmulationErrorRoutedEventArgs>(nameof(EmulationError), Avalonia.Interactivity.RoutingStrategies.Bubble);
    public static readonly RoutedEvent<RoutedEventArgs> CurrentProcessChangedEvent =
        RoutedEvent.Register<EmulatorDisplay, RoutedEventArgs>(nameof(CurrentProcessChanged), Avalonia.Interactivity.RoutingStrategies.Bubble);

    private EmulatorHost? emulator;
    private bool mouseJustCaptured;
    private bool isMouseCaptured;
    private Point centerPoint;
    private DispatcherTimer? timer;
    private readonly SimpleCommand resumeCommand;
    private readonly SimpleCommand pauseCommand;
    private VideoRenderer? aeonRenderer;
    private WriteableBitmap? renderTarget;
    private int renderTargetWidth;
    private int renderTargetHeight;

    static EmulatorDisplay()
    {
        EmulationSpeedProperty.Changed.AddClassHandler<EmulatorDisplay>(OnEmulationSpeedChanged);
        IsAspectRatioLockedProperty.Changed.AddClassHandler<EmulatorDisplay>(OnIsAspectRatioLockedChanged);
        ScalingAlgorithmProperty.Changed.AddClassHandler<EmulatorDisplay>(OnScalingAlgorithmChanged);
    }

    public EmulatorDisplay()
    {
        this.resumeCommand = new SimpleCommand(() => this.EmulatorState == EmulatorState.Paused, () => { this.EmulatorHost?.Run(); });
        this.pauseCommand = new SimpleCommand(() => this.EmulatorState == EmulatorState.Running, () => { this.EmulatorHost?.Pause(); });

        InitializeComponent();

        this.displayImage.PointerPressed += DisplayImage_PointerPressed;
        this.displayImage.PointerReleased += DisplayImage_PointerReleased;
        this.displayImage.PointerMoved += DisplayImage_PointerMoved;
    }

    public event EventHandler<RoutedEventArgs>? EmulatorStateChanged
    {
        add => this.AddHandler(EmulatorStateChangedEvent, value);
        remove => this.RemoveHandler(EmulatorStateChangedEvent, value);
    }
    public event EventHandler<EmulationErrorRoutedEventArgs>? EmulationError
    {
        add => this.AddHandler(EmulationErrorEvent, value);
        remove => this.RemoveHandler(EmulationErrorEvent, value);
    }
    public event EventHandler<RoutedEventArgs>? CurrentProcessChanged
    {
        add => this.AddHandler(CurrentProcessChangedEvent, value);
        remove => this.RemoveHandler(CurrentProcessChangedEvent, value);
    }

    public EmulatorHost? EmulatorHost
    {
        get => this.emulator;
        set
        {
            if (!ReferenceEquals(this.emulator, value))
            {
                if (this.emulator is not null)
                {
                    this.emulator.VideoModeChanged -= this.HandleModeChange;
                    this.emulator.StateChanged -= this.Emulator_StateChanged;
                    this.emulator.Error -= this.Emulator_Error;
                    this.emulator.CurrentProcessChanged -= this.Emulator_CurrentProcessChanged;
                    this.timer?.Stop();
                    this.emulator.Dispose();
                }

                this.emulator = value;

                if (this.emulator is not null)
                {
                    this.emulator.VideoModeChanged += this.HandleModeChange;
                    this.emulator.StateChanged += this.Emulator_StateChanged;
                    this.emulator.Error += this.Emulator_Error;
                    this.emulator.CurrentProcessChanged += this.Emulator_CurrentProcessChanged;
                    this.emulator.EmulationSpeed = this.EmulationSpeed;
                    this.EnsureTimer();
                    this.timer!.Start();
                    this.InitializePresenter();
                }
            }
        }
    }

    public EmulatorState EmulatorState
    {
        get => GetValue(EmulatorStateProperty);
        private set => SetValue(EmulatorStateProperty, value);
    }

    public MouseInputMode MouseInputMode
    {
        get => GetValue(MouseInputModeProperty);
        set => SetValue(MouseInputModeProperty, value);
    }

    public bool IsMouseCursorCaptured
    {
        get => GetValue(IsMouseCursorCapturedProperty);
        private set => SetValue(IsMouseCursorCapturedProperty, value);
    }

    public int EmulationSpeed
    {
        get => GetValue(EmulationSpeedProperty);
        set => SetValue(EmulationSpeedProperty, value);
    }

    public bool IsAspectRatioLocked
    {
        get => GetValue(IsAspectRatioLockedProperty);
        set => SetValue(IsAspectRatioLockedProperty, value);
    }

    public ScalingAlgorithm ScalingAlgorithm
    {
        get => GetValue(ScalingAlgorithmProperty);
        set => SetValue(ScalingAlgorithmProperty, value);
    }

    public WriteableBitmap? DisplayBitmap => this.renderTarget;
    public Emulator.Dos.DosProcess? CurrentProcess { get; private set; }
    public ICommand ResumeCommand => this.resumeCommand;
    public ICommand PauseCommand => this.pauseCommand;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.EnsureTimer();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Handled by MainWindow
        }

        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (e.Key == Key.F12 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                this.isMouseCaptured = false;
                this.IsMouseCursorCaptured = false;
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
        this.IsMouseCursorCaptured = false;
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
            this.emulator.ReleaseAllKeys();

        base.OnLostFocus(e);
    }

    private void EnsureTimer()
    {
        this.timer ??= new DispatcherTimer(TimeSpan.FromSeconds(1.0 / 60.0), DispatcherPriority.Render, GraphicalUpdate);
    }

    private void GraphicalUpdate(object? sender, EventArgs e)
    {
        if (this.emulator != null)
        {
            var presenter = this.aeonRenderer;
            if (presenter == null)
                return;

            this.EnsureRenderTarget(presenter);

            if (this.renderTarget != null)
            {
                using var fb = this.renderTarget.Lock();
                unsafe
                {
                    var span = new Span<uint>(fb.Address.ToPointer(), this.renderTargetWidth * this.renderTargetHeight);
                    presenter.Draw(span);
                }
            }

            this.displayImage.InvalidateVisual();
        }
    }

    private void HandleModeChange(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => this.InitializePresenter());

    private void InitializePresenter()
    {
        this.displayImage.Source = null;
        if (this.emulator == null)
            return;

        this.aeonRenderer = this.emulator.VirtualMachine.GetRenderer<PixelFormatBGRA>();
        if (this.aeonRenderer == null)
            return;

        this.EnsureRenderTarget(this.aeonRenderer);

        int pixelWidth = this.aeonRenderer.Width;
        int pixelHeight = this.aeonRenderer.Height;
        this.displayImage.Source = this.renderTarget;
        this.displayImage.Width = pixelWidth;
        this.displayImage.Height = pixelHeight;
        this.displayArea.Width = pixelWidth;
        this.displayArea.Height = pixelHeight;

        this.centerPoint = new Point(pixelWidth / 2, pixelHeight / 2);
    }

    private void EnsureRenderTarget(VideoRenderer presenter)
    {
        if (this.renderTarget == null || presenter.Width != this.renderTargetWidth || presenter.Height != this.renderTargetHeight)
        {
            this.renderTarget?.Dispose();
            this.renderTargetWidth = presenter.Width;
            this.renderTargetHeight = presenter.Height;
            this.renderTarget = new WriteableBitmap(
                new PixelSize(this.renderTargetWidth, this.renderTargetHeight),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                AlphaFormat.Opaque);
        }
    }

    private static void OnEmulationSpeedChanged(EmulatorDisplay obj, AvaloniaPropertyChangedEventArgs e)
    {
        var newValue = (int)e.NewValue!;
        if (newValue < EmulatorHost.MinimumSpeed)
        {
            obj.SetValue(EmulationSpeedProperty, EmulatorHost.MinimumSpeed);
            return;
        }
        if (obj.emulator != null)
            obj.emulator.EmulationSpeed = newValue;
    }

    private static void OnIsAspectRatioLockedChanged(EmulatorDisplay obj, AvaloniaPropertyChangedEventArgs e)
    {
        bool value = (bool)e.NewValue!;
        obj.outerViewbox.Stretch = value ? Stretch.Uniform : Stretch.Fill;
    }

    private static void OnScalingAlgorithmChanged(EmulatorDisplay obj, AvaloniaPropertyChangedEventArgs e)
    {
        obj.InitializePresenter();
    }

    private void Emulator_StateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (this.emulator != null)
            {
                this.EmulatorState = this.emulator.State;
                this.resumeCommand.UpdateState();
                this.pauseCommand.UpdateState();
                this.RaiseEvent(new RoutedEventArgs(EmulatorStateChangedEvent));
            }
        });
    }

    private void Emulator_Error(object? sender, ErrorEventArgs e)
    {
        Dispatcher.UIThread.Post(() => this.RaiseEvent(new EmulationErrorRoutedEventArgs(EmulationErrorEvent, e.Message)));
    }

    private void Emulator_CurrentProcessChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (this.emulator != null)
                this.CurrentProcess = this.emulator.VirtualMachine.CurrentProcess;
            else
                this.CurrentProcess = null;

            this.RaiseEvent(new RoutedEventArgs(CurrentProcessChangedEvent));
        });
    }

    private void DisplayImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (!this.isMouseCaptured && this.MouseInputMode == MouseInputMode.Relative)
            {
                this.IsMouseCursorCaptured = true;
                this.mouseJustCaptured = true;
                this.isMouseCaptured = true;

                this.centerPoint = new Point(displayImage.Width / 2, displayImage.Height / 2);
                return;
            }

            var props = e.GetCurrentPoint(this.displayImage).Properties;
            var button = props.PointerUpdateKind.ToEmulatorButtons();
            if (button != MouseButtons.None)
            {
                var mouseEvent = new MouseButtonDownEvent(button);
                this.emulator.MouseEvent(mouseEvent);
            }
        }
    }

    private void DisplayImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (this.mouseJustCaptured)
            {
                this.mouseJustCaptured = false;
                return;
            }

            var props = e.GetCurrentPoint(this.displayImage).Properties;
            var button = props.PointerUpdateKind.ToEmulatorButtons();
            if (button != MouseButtons.None)
            {
                var mouseEvent = new MouseButtonUpEvent(button);
                this.emulator.MouseEvent(mouseEvent);
            }
        }
    }

    private void DisplayImage_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (this.emulator != null && this.emulator.State == EmulatorState.Running)
        {
            if (this.MouseInputMode == MouseInputMode.Absolute)
            {
                var pos = e.GetPosition(displayImage);
                this.emulator.MouseEvent(new MouseMoveAbsoluteEvent((int)pos.X, (int)pos.Y));
            }
            else if (this.isMouseCaptured)
            {
                var pos = e.GetPosition(this.displayImage);

                int dx = (int)(pos.X - this.centerPoint.X);
                int dy = (int)(pos.Y - this.centerPoint.Y);

                if (dx != 0 || dy != 0)
                {
                    this.emulator.MouseEvent(new MouseMoveRelativeEvent(dx, dy));
                    var screenPoint = this.displayImage.PointToScreen(centerPoint);
                    CursorHelper.WarpCursor((int)screenPoint.X, (int)screenPoint.Y);
                }
            }
        }
    }
}
