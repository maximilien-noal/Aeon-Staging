using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Aeon.Emulator.Launcher.Behaviors;

public sealed class CopyScreenToClipboardBehavior : Behavior<MenuItem>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        this.AssociatedObject?.Click += this.OnMenuItemClick;
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject?.Click -= this.OnMenuItemClick;

        base.OnDetaching();
    }

    private async void OnMenuItemClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popupRoot = TopLevel.GetTopLevel(this.AssociatedObject) as PopupRoot;
        var mainWindow = popupRoot?.ParentTopLevel;
        if (mainWindow?.Clipboard == null)
            return;

        var imageControl = mainWindow.GetVisualDescendants()
            .OfType<Image>()
            .FirstOrDefault(img => img.Name == "displayImage");

        if (imageControl?.Source is not Bitmap sourceBitmap)
            return;

        await CopyToClipboardAsync(sourceBitmap, mainWindow.Clipboard);
    }

    public static async Task CopyToClipboardAsync(Bitmap sourceBitmap, IClipboard clipboard)
    {
        using var stream = new MemoryStream();
        sourceBitmap.Save(stream);
        stream.Position = 0;
        using var snapshot = new Bitmap(stream);

        if (OperatingSystem.IsWindows())
        {
            WindowsClipboard.SetBitmap(snapshot);
        }
        else
        {
            await clipboard.SetBitmapAsync(snapshot);
            await clipboard.FlushAsync();
        }
    }
}
