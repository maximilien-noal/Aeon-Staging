using Aeon.Emulator.Launcher;

namespace Aeon.UI.Tests.StepDefinitions;

/// <summary>
/// Shared scenario context for passing state between step definition classes.
/// Reqnroll injects this via constructor injection, scoped per scenario.
/// </summary>
public sealed class SharedState
{
    public MainWindow? MainWindow { get; set; }
}
