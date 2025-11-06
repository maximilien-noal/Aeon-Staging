using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Aeon.Emulator.DebugSupport;
using Avalonia.Markup.Xaml;

namespace Aeon.Emulator.Launcher.Debugger
{
    /// <summary>
    /// Provides a view of disassembled machine code instructions.
    /// </summary>
    public partial class DisassemblyView : UserControl
    {
        /// <summary>
        /// The AddressClick routed event definition.
        /// </summary>
        public static readonly RoutedEvent AddressClickEvent = RoutedEvent.Register(nameof(AddressClick), RoutingStrategy.Bubble, typeof(EventHandler<AddressClickEventArgs>), typeof(DisassemblyView));
        /// <summary>
        /// The InstructionsSource dependency property definition.
        /// </summary>
        public static readonly StyledProperty InstructionsSourceProperty = AvaloniaProperty.Register<nameof(InstructionsSource), typeof(IEnumerable<Instruction>), typeof(DisassemblyView));
        /// <summary>
        /// The IsHexFormat dependency property definition.
        /// </summary>
        public static readonly StyledProperty IsHexFormatProperty = AeonDebug.IsHexFormatProperty.AddOwner(typeof(DisassemblyView));
        /// <summary>
        /// The DebuggerTextFormat dependency property definition.
        /// </summary>
        public static readonly StyledProperty DebuggerTextFormatProperty = AeonDebug.DebuggerTextFormatProperty.AddOwner(typeof(DisassemblyView));

        /// <summary>
        /// Initializes a new instance of the <see cref="DisassemblyView"/> class.
        /// </summary>
        public DisassemblyView()
        {
            AvaloniaXamlLoader.Load(this);
            this.AddHandler(Hyperlink.ClickEvent, new RoutedEventHandler(this.Hyperlink_Click));
        }

        /// <summary>
        /// Occurs when an address has been clicked. This is a dependency property.
        /// </summary>
        public event EventHandler<AddressClickEventArgs> AddressClick
        {
            add { this.AddHandler(AddressClickEvent, value); }
            remove { this.RemoveHandler(AddressClickEvent, value); }
        }

        /// <summary>
        /// Gets or sets the instructions used. This is a dependency property.
        /// </summary>
        public IEnumerable<Instruction> InstructionsSource
        {
            get => (IEnumerable<Instruction>)this.GetValue(InstructionsSourceProperty);
            set => this.SetValue(InstructionsSourceProperty, value);
        }
        /// <summary>
        /// Gets or sets a value indicating whether immediate values should be displayed in hexadecimal. This is a dependency property.
        /// </summary>
        public bool IsHexFormat
        {
            get => (bool)this.GetValue(IsHexFormatProperty);
            set => this.SetValue(IsHexFormatProperty, value);
        }
        /// <summary>
        /// Gets or sets formatting information. This is a dependency property.
        /// </summary>
        public IDebuggerTextSettings DebuggerTextFormat
        {
            get => (IDebuggerTextSettings)this.GetValue(DebuggerTextFormatProperty);
            set => this.SetValue(DebuggerTextFormatProperty, value);
        }

        /// <summary>
        /// Raises the AddressClick event.
        /// </summary>
        /// <param name="e">Information about the event.</param>
        protected virtual void OnAddressClick(AddressClickEventArgs e)
        {
            this.RaiseEvent(e);

            if (!e.Handled)
            {
                if (e.Target.AddressType == TargetAddressType.Code)
                {
                    var disasm = this.InstructionsSource;
                    var inst = disasm.Where(i => i.EIP == e.Target.Address.Offset && i.CS == e.Target.Address.Segment).FirstOrDefault();
                    if (inst != null)
                    {
                        this.listBox.SelectedItem = inst;
                        this.listBox.ScrollIntoView(inst);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when a hyperlink has been clicked.
        /// </summary>
        /// <param name="source">Source of the event.</param>
        /// <param name="e">Information about the event.</param>
        private void Hyperlink_Click(object source, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Hyperlink hyperlink)
                return;

            if (hyperlink.Tag is not TargetAddress address)
                return;

            e.Handled = true;
            this.OnAddressClick(new AddressClickEventArgs(address, AddressClickEvent));
        }
    }
}
