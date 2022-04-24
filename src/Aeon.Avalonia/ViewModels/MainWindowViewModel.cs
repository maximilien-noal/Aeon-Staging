using System;
using System.Collections.Generic;
using System.Text;

namespace Aeon.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    public string[] Args { get; internal set; }
}
