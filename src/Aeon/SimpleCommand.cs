using System.Windows.Input;

namespace Aeon.Emulator.Launcher;

internal sealed class SimpleCommand : ICommand
{
    private readonly Func<bool> canExecute;
    private readonly Action execute;
    private bool canExecuteState;

    public SimpleCommand(Func<bool> canExecute, Action execute)
    {
        this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecuteState = canExecute();
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => this.canExecuteState;
    public void Execute(object? parameter) => this.execute();
    public void UpdateState()
    {
        bool newState = this.canExecute();
        if (this.canExecuteState != newState)
        {
            this.canExecuteState = newState;
            OnCanExecuteChanged(EventArgs.Empty);
        }
    }

    private void OnCanExecuteChanged(EventArgs e) => this.CanExecuteChanged?.Invoke(this, e);
}
