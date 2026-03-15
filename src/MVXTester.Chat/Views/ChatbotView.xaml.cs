using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MVXTester.Chat.Views;

public partial class ChatbotView : UserControl
{
    public ChatbotView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Auto-scroll when new messages are added
        if (DataContext is ViewModels.ChatbotViewModel vm)
        {
            vm.Messages.CollectionChanged += OnMessagesChanged;
        }
        InputTextBox.PreviewKeyDown += OnInputPreviewKeyDown;
        IsVisibleChanged += OnVisibilityChanged;

        // 부모 Window가 활성화될 때(Show/focus)도 자동 포커스
        var parentWindow = Window.GetWindow(this);
        if (parentWindow != null)
            parentWindow.Activated += OnParentWindowActivated;

        FocusInput();
    }

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            FocusInput();
    }

    private void OnParentWindowActivated(object? sender, EventArgs e)
    {
        FocusInput();
    }

    private void FocusInput()
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
            () => { InputTextBox.Focus(); Keyboard.Focus(InputTextBox); });
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.ChatbotViewModel vm) return;

        // Enter → Send, Shift+Enter → Newline
        if (e.Key == Key.Return && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            if (vm.SendMessageCommand.CanExecute(null))
                vm.SendMessageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Intercept Ctrl+V when clipboard contains an image
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control
            && Clipboard.ContainsImage())
        {
            var bitmapSource = Clipboard.GetImage();
            if (bitmapSource != null)
            {
                vm.PasteImageFromClipboard(bitmapSource);
                e.Handled = true;
            }
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to bottom on new messages
        MessageScrollViewer.ScrollToEnd();
        FocusInput();
    }

    private void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
        e.Handled = true;
    }
}
