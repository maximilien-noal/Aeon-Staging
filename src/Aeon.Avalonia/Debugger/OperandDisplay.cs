using System;

using Aeon.Emulator.DebugSupport;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Aeon.Emulator.Launcher.Debugger
{
    /// <summary>
    /// Visual representation of an instruction operand.
    /// </summary>
    internal sealed class OperandDisplay : ContentControl
    {
        /// <summary>
        /// The DebuggerTextFormat dependency property definition.
        /// </summary>
        public static readonly AvaloniaProperty<IDebuggerTextSettings> DebuggerTextFormatProperty = AvaloniaProperty
            .RegisterAttached<AeonDebug, IDebuggerTextSettings>(nameof(AeonDebug.DebuggerTextSettingsProperty), typeof(OperandDisplay));

        /// <summary>
        /// The Instruction dependency property definition.
        /// </summary>
        public static readonly StyledProperty<Instruction> InstructionProperty = AvaloniaProperty
            .Register<OperandDisplay, Instruction>(nameof(Instruction));

        /// <summary>
        /// The IsHexFormat dependency property definition.
        /// </summary>
        public static readonly StyledProperty<bool> IsHexFormatProperty = AvaloniaProperty
            .RegisterAttached<AeonDebug, bool>(nameof(AeonDebug.IsHexFormatProperty), typeof(OperandDisplay));

        /// <summary>
        /// The Operand dependency property definition.
        /// </summary>
        public static readonly StyledProperty<CodeOperand> OperandProperty =
            AvaloniaProperty.Register<OperandDisplay, CodeOperand>(nameof(Operand));

        /// <summary>
        /// The RegisterSource dependency property definition.
        /// </summary>
        public static readonly StyledProperty<IRegisterContainer> RegisterSourceProperty =
            AvaloniaProperty.Register<OperandDisplay, IRegisterContainer>(nameof(RegisterSource));

        private const PrefixState SegmentPrefixes = PrefixState.CS | PrefixState.DS | PrefixState.ES | PrefixState.FS | PrefixState.GS | PrefixState.SS;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperandDisplay"/> class.
        /// </summary>
        public OperandDisplay()
        {
        }

        /// <summary>
        /// Gets or sets formatting information. This is a dependency property.
        /// </summary>
        public IDebuggerTextSettings? DebuggerTextFormat
        {
            get => (IDebuggerTextSettings?)this.GetValue(DebuggerTextFormatProperty);
            set => this.SetValue(DebuggerTextFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the instruction containing the displayed operand. This is a dependency property.
        /// </summary>
        public Instruction Instruction
        {
            get => (Instruction)this.GetValue(InstructionProperty);
            set => this.SetValue(InstructionProperty, value);
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
        /// Gets or sets the operand to display. This is a dependency property.
        /// </summary>
        public CodeOperand Operand
        {
            get => (CodeOperand)this.GetValue(OperandProperty);
            set => this.SetValue(OperandProperty, value);
        }

        /// <summary>
        /// Gets or sets the source for displayed register values. This is a dependency property.
        /// </summary>
        public IRegisterContainer RegisterSource
        {
            get => (IRegisterContainer)this.GetValue(RegisterSourceProperty);
            set => this.SetValue(RegisterSourceProperty, value);
        }

        /// <summary>
        /// Invoked when a property value has changed.
        /// </summary>
        /// <param name="e">Information about the event.</param>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == OperandProperty || e.Property == AeonDebug.IsHexFormatProperty || e.Property == InstructionProperty)
                this.UpdateContent();
        }

        /// <summary>
        /// Returns a string representation of a segment register.
        /// </summary>
        /// <param name="prefixes">Segment register prefix.</param>
        /// <returns>String representation of the segment register.</returns>
        private static string GetSegmentPrefix(PrefixState prefixes)
        {
            if ((prefixes & SegmentPrefixes) != 0)
                return (prefixes & SegmentPrefixes).ToString().ToLower();
            else
                return null;
        }

        private Control BuildAddress16Content(CodeOperand operand)
        {
            bool includeDisplacement = true;
            var textBlock = new TextBlock();

            var prefixes = this.Instruction.Prefixes;
            if (operand.OperandSize == CodeOperandSize.Byte)
                textBlock.Text += "byte ptr ";
            else if (operand.OperandSize == CodeOperandSize.Word && (prefixes & PrefixState.OperandSize) == 0)
                textBlock.Text += "word ptr ";
            else
                textBlock.Text += "dword ptr ";

            var segmentOverride = GetSegmentPrefix(prefixes);
            if (segmentOverride != null)
            {
                textBlock.Text += NewRegisterRun(segmentOverride);
                textBlock.Text += ":";
            }

            textBlock.Text += "[";

            switch (operand.EffectiveAddress)
            {
                case CodeMemoryBase.DisplacementOnly:
                    textBlock.Text += NewImmediateRun(operand.ImmediateValue.ToString("X4"));
                    includeDisplacement = false;
                    break;

                case CodeMemoryBase.BX_plus_SI:
                    textBlock.Text += NewRegisterRun("bx");
                    textBlock.Text += "+";
                    textBlock.Text += NewRegisterRun("si");
                    break;

                case CodeMemoryBase.BX_plus_DI:
                    textBlock.Text += NewRegisterRun("bx");
                    textBlock.Text += "+";
                    textBlock.Text += NewRegisterRun("di");
                    break;

                case CodeMemoryBase.BP_plus_SI:
                    textBlock.Text += NewRegisterRun("bp");
                    textBlock.Text += "+";
                    textBlock.Text += NewRegisterRun("si");
                    break;

                case CodeMemoryBase.BP_plus_DI:
                    textBlock.Text += NewRegisterRun("bp");
                    textBlock.Text += "+";
                    textBlock.Text += NewRegisterRun("di");
                    break;

                case CodeMemoryBase.SI:
                    textBlock.Text += NewRegisterRun("si");
                    break;

                case CodeMemoryBase.DI:
                    textBlock.Text += NewRegisterRun("di");
                    break;

                case CodeMemoryBase.BX:
                    textBlock.Text += NewRegisterRun("bx");
                    break;

                case CodeMemoryBase.BP:
                    textBlock.Text += NewRegisterRun("bp");
                    break;
            }

            if (includeDisplacement && operand.ImmediateValue != 0)
            {
                int offset = (short)(ushort)operand.ImmediateValue;
                textBlock.Text += offset >= 0 ? "+" : "-";
                textBlock.Text += NewDisplacementRun(Math.Abs(offset).ToString());
            }

            textBlock.Text += "]";

            return textBlock;
        }

        /// <summary>
        /// Rebuilds the displayed content.
        /// </summary>
        /// <param name="operand">Operand to display.</param>
        /// <returns>Control to display as content.</returns>
        private Control BuildContent(CodeOperand operand)
        {
            return operand.Type switch
            {
                CodeOperandType.Immediate => this.BuildImmediateContent(operand.ImmediateValue),
                CodeOperandType.Register => this.BuildRegisterContent(operand.RegisterValue),
                CodeOperandType.MemoryAddress => this.BuildAddress16Content(operand),
                CodeOperandType.RelativeJumpAddress => this.BuildJumpTargetContent((uint)(this.Instruction.EIP + this.Instruction.Length + (int)operand.ImmediateValue)),
                _ => null
            };
        }

        private Control BuildImmediateContent(uint value)
        {
            var textBlock = new TextBlock();
            if (this.IsHexFormat)
                textBlock.Text = value.ToString("X");
            else
                textBlock.Text = value.ToString();

            var binding = new Binding("DebuggerTextFormat.Immediate") { Source = this, Mode = BindingMode.OneWay };
            textBlock.Bind(TextBlock.ForegroundProperty, binding);

            return textBlock;
        }

        private Control BuildJumpTargetContent(uint offset)
        {
            var textBlock = new TextBlock();
            var run = new TextBlock() { Text = offset.ToString("X8") };
            {
                Tag = new TargetAddress(QualifiedAddress.FromRealModeAddress(this.Instruction.CS, (ushort)offset), TargetAddressType.Code);
            }

            var binding = new Binding("DebuggerTextFormat.Address") { Source = this, Mode = BindingMode.OneWay };
            run.Bind(TextBlock.ForegroundProperty, binding);

            textBlock.Text += run.Text;
            return textBlock;
        }

        private Control BuildRegisterContent(CodeRegister register)
        {
            var textBlock = new TextBlock() { Text = register.ToString().ToLower() };

            var binding = new Binding("DebuggerTextFormat.Register") { Source = this, Mode = BindingMode.OneWay };
            textBlock.Bind(TextBlock.ForegroundProperty, binding);

            return textBlock;
        }

        private TextBlock NewDisplacementRun(string text)
        {
            var run = new TextBlock() { Text = text };

            var binding = new Binding("DebuggerTextFormat.Address") { Source = this, Mode = BindingMode.OneWay };
            run.Bind(TextBlock.ForegroundProperty, binding);

            return run;
        }

        private TextBlock NewImmediateRun(string text)
        {
            var run = new TextBlock() { Text = text };

            var binding = new Binding("DebuggerTextFormat.Immediate") { Source = this, Mode = BindingMode.OneWay };
            run.Bind(TextBlock.ForegroundProperty, binding);

            return run;
        }

        private TextBlock NewRegisterRun(string text)
        {
            var run = new TextBlock() { Text = text };

            var binding = new Binding("DebuggerTextFormat.Register") { Source = this, Mode = BindingMode.OneWay };
            run.Bind(TextBlock.ForegroundProperty, binding);

            return run;
        }

        /// <summary>
        /// Rebuilds the displayed content.
        /// </summary>
        private void UpdateContent()
        {
            if (this.Instruction != null)
                this.Content = this.BuildContent(this.Operand);
            else
                this.Content = null;
        }
    }
}