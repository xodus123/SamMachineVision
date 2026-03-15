using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MVXTester.App.ViewModels;

namespace MVXTester.App.Views;

public partial class PropertyEditorView : UserControl
{
    public PropertyEditorView()
    {
        InitializeComponent();
    }

    private void ResultImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not PropertyEditorViewModel vm) return;
        var pos = e.GetPosition(ResultImageElement);
        var size = ResultImageElement.RenderSize;
        vm.OnImageMouseMove(pos.X, pos.Y, size.Width, size.Height);
    }

    private void ResultImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not PropertyEditorViewModel vm) return;
        var pos = e.GetPosition(ResultImageElement);
        var size = ResultImageElement.RenderSize;

        if (e.ClickCount == 2)
        {
            // Double-click: reset ROI to full image
            vm.OnImageDoubleClick();
            e.Handled = true;
            return;
        }

        vm.OnImageMouseDown(pos.X, pos.Y, size.Width, size.Height);
        ((UIElement)sender).CaptureMouse();
    }

    private void ResultImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not PropertyEditorViewModel vm) return;
        var pos = e.GetPosition(ResultImageElement);
        var size = ResultImageElement.RenderSize;
        vm.OnImageMouseUp(pos.X, pos.Y, size.Width, size.Height);
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void ResultImage_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is not PropertyEditorViewModel vm) return;
        vm.OnImageMouseLeave();
    }
}
