# XAML Migration Guide: WPF to Avalonia

## Current Status
- **C# Code**: ✅ 100% Complete (0 compilation errors)
- **XAML Files**: ❌ 488 errors across 14 files requiring manual conversion

## Error Summary by File

| File | Errors | Primary Issues |
|------|--------|----------------|
| MainWindow.axaml | 200 | CommandBinding, ToolBarTray, ToolBar, ObjectDataProvider |
| InstructionTemplates.axaml | 128 | DataTrigger, Trigger (in templates) |
| PerformanceWindow.axaml | 40 | DataTrigger, Style patterns |
| RegisterViewer.axaml | 36 | DataTrigger patterns |
| InstructionLogWindow.axaml | 22 | ToolBarTray, ToolBar |
| EmulatorDisplay.axaml | 12 | UserControl patterns |
| TaskDialogTemplates.axaml | 10 | Style TargetType, KeyValuePair |
| DisassemblyView.axaml | 10 | ItemContainerStyleSelector, Style TargetType |
| EmulatorDisplayResources.axaml | 8 | Style patterns |
| DebuggerWindow.axaml | 8 | ToolBar |
| MemoryView.axaml | 6 | Minor binding issues |
| ListBoxTemplates.axaml | 4 | Style patterns |
| TaskDialog.axaml | 2 | Minor issues |
| RoundButtonResources.axaml | 2 | ControlTemplate |

## Critical WPF Features Requiring Conversion

### 1. CommandBinding (70+ instances in MainWindow.axaml)

**WPF Pattern:**
```xml
<Window.CommandBindings>
    <CommandBinding Command="Close" CanExecute="Close_CanExecute" Executed="Close_Executed" />
</Window.CommandBindings>
```

**Avalonia Solutions:**

**Option A:** Use KeyBindings for keyboard shortcuts
```xml
<Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+Q" Command="{Binding CloseCommand}" />
</Window.KeyBindings>
```

**Option B:** Bind Command property directly
```xml
<MenuItem Header="E_xit" Command="{Binding CloseCommand}" />
```

**Option C:** Use code-behind Click handlers (already in place)
```xml
<MenuItem Header="E_xit" Click="Close_Click" />
```

**Recommendation**: Keep existing Click handlers, add KeyBindings for shortcuts.

### 2. ToolBarTray / ToolBar (68+ instances)

**Problem**: Avalonia doesn't have ToolBarTray or ToolBar controls.

**Avalonia Alternative:**
```xml
<!-- Replace ToolBarTray with StackPanel or Border -->
<Border Background="{StaticResource toolbarBackground}">
    <StackPanel Orientation="Horizontal" Margin="4,2">
        <!-- Toolbar items here -->
        <Button ToolTip="Run Program..." Click="QuickLaunch_Click">
            <Image Width="16" Height="16" Source="Resources/openfolderHS.png" />
        </Button>
        <Separator />
        <Button Command="{Binding ResumeCommand}" ToolTip="Resume">
            <!-- Button content -->
        </Button>
    </StackPanel>
</Border>
```

### 3. DataTrigger (140+ instances)

**Problem**: Avalonia doesn't support DataTrigger in DataTemplates.

**Avalonia Solutions:**

**Option A:** Use MultiBinding with ValueConverter
```xml
<TextBlock>
    <TextBlock.IsVisible>
        <MultiBinding Converter="{StaticResource operandCountToVisibilityConverter}">
            <Binding Path="Opcode.Operands.Count" />
            <Binding Source="2" />
        </MultiBinding>
    </TextBlock.IsVisible>
</TextBlock>
```

**Option B:** Use code-behind or ViewModel properties
```csharp
public bool ShowComma1 => Opcode.Operands.Count >= 2;
```

```xml
<TextBlock Text=", " IsVisible="{Binding ShowComma1}" />
```

**Option C:** Use Avalonia's reactive extensions (more complex)

**Recommendation**: Option B (ViewModel properties) for simplicity.

### 4. Style TargetType and Triggers

**WPF Pattern:**
```xml
<Style x:Key="myStyle" TargetType="ListBoxItem" BasedOn="{StaticResource baseStyle}">
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="Blue" />
        </Trigger>
    </Style.Triggers>
</Style>
```

**Avalonia Pattern:**
```xml
<Style Selector="ListBoxItem">
    <Setter Property="Background" Value="Transparent" />
</Style>
<Style Selector="ListBoxItem:pointerover">
    <Setter Property="Background" Value="Blue" />
</Style>
```

### 5. ItemContainerStyleSelector

**WPF Pattern:**
```xml
<ListBox ItemContainerStyleSelector="{StaticResource instructionStyleSelector}" />
```

**Avalonia Alternative:**
```xml
<!-- Use classes and conditional styles -->
<ListBox>
    <ListBox.Styles>
        <Style Selector="ListBoxItem.breakpoint">
            <Setter Property="Background" Value="Red" />
        </Style>
        <Style Selector="ListBoxItem.current">
            <Setter Property="Background" Value="Yellow" />
        </Style>
    </ListBox.Styles>
</ListBox>
```

Then add classes dynamically in code-behind or via binding.

### 6. ObjectDataProvider

**WPF Pattern:**
```xml
<ObjectDataProvider x:Key="scalerValues" MethodName="GetValues" ObjectType="{x:Type sys:Enum}">
    <ObjectDataProvider.MethodParameters>
        <x:Type TypeName="rendering:ScalingAlgorithm" />
    </ObjectDataProvider.MethodParameters>
</ObjectDataProvider>
```

**Avalonia Alternative:**
```csharp
// In ViewModel or code-behind
public IEnumerable<ScalingAlgorithm> ScalerValues => 
    Enum.GetValues(typeof(ScalingAlgorithm)).Cast<ScalingAlgorithm>();
```

```xml
<ComboBox ItemsSource="{Binding ScalerValues}" />
```

### 7. ControlTemplate.Triggers

**Problem**: Avalonia doesn't support Triggers in ControlTemplates.

**Solution**: Use Styles with pseudo-classes
```xml
<Style Selector="CheckBox:checked /template/ Rectangle#verticalLine">
    <Setter Property="IsVisible" Value="True" />
</Style>
<Style Selector="CheckBox:pointerover /template/ Rectangle#horizontalLine">
    <Setter Property="Fill" Value="Blue" />
</Style>
```

## Step-by-Step Migration Plan

### Phase 1: MainWindow.axaml (Most Critical - 200 errors)

1. **Remove CommandBindings** - Use existing Click handlers
2. **Replace ToolBarTray/ToolBar** - Use Border + StackPanel
3. **Remove ObjectDataProvider** - Move to ViewModel/code-behind
4. **Comment out BoolToVisibilityConverter** - Use IsVisible directly

### Phase 2: InstructionTemplates.axaml (128 errors)

1. **Convert DataTriggers** - Add ViewModel properties for visibility
2. **Convert ControlTemplate.Triggers** - Use Styles with selectors
3. **Test template rendering**

### Phase 3: Other Windows (110 errors total)

1. **PerformanceWindow.axaml** - DataTriggers to ViewModel
2. **RegisterViewer.axaml** - DataTriggers to ViewModel
3. **InstructionLogWindow.axaml** - ToolBar replacement
4. **DebuggerWindow.axaml** - ToolBar replacement

### Phase 4: Resources and Templates (36 errors)

1. **TaskDialogTemplates.axaml** - Fix Style syntax
2. **DisassemblyView.axaml** - Remove ItemContainerStyleSelector
3. **EmulatorDisplayResources.axaml** - Style syntax
4. **ListBoxTemplates.axaml** - Style syntax
5. **RoundButtonResources.axaml** - ControlTemplate fixes

## Implementation Notes

### Required C# Changes

1. **Add ViewModel Properties** for DataTrigger replacements
2. **Create ValueConverters** for complex bindings
3. **Update InstructionTemplateSelector** to work without ItemTemplateSelector property
4. **Move ObjectDataProvider logic** to code-behind

### Testing Strategy

1. Fix one file at a time
2. Build after each fix to verify error reduction
3. Test UI rendering in running application
4. Verify functionality (buttons, menus, commands)

## Estimated Effort

- **MainWindow.axaml**: 4-6 hours (complex toolbar and command system)
- **InstructionTemplates.axaml**: 3-4 hours (many DataTriggers)
- **Other XAML files**: 3-4 hours (simpler patterns)
- **Testing and refinement**: 2-3 hours
- **Total**: 12-17 hours of focused development

## Alternative Approach

If timeline is critical, consider:
1. **Simplified UI**: Remove toolbar, use simpler menu-only interface
2. **Basic Templates**: Remove complex trigger-based styling
3. **Gradual Migration**: Ship with basic UI, enhance over time

## Conclusion

The XAML migration requires significant manual work because:
- WPF and Avalonia have fundamentally different approaches to styling and triggers
- No automated conversion tool can make UI/UX decisions
- Each DataTrigger requires understanding the business logic to convert properly
- Toolbar replacement requires redesigning the UI layout

This is expected for a major UI framework migration and represents the final 10-15% of the overall migration effort.
