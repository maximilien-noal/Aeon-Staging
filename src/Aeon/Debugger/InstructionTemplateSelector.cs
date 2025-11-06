using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Aeon.Emulator.DebugSupport;

namespace Aeon.Emulator.Launcher.Debugger
{
    internal sealed class InstructionTemplateSelector : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is LoggedInstruction)
            {
                // Return logged instruction template
                var template = Application.Current?.FindResource("loggedInstructionTemplate") as IDataTemplate;
                return template?.Build(data);
            }
            else
            {
                // Return regular instruction template
                var template = Application.Current?.FindResource("instructionTemplate") as IDataTemplate;
                return template?.Build(data);
            }
        }

        public bool Match(object? data)
        {
            return data is Instruction || data is LoggedInstruction;
        }
    }
}
