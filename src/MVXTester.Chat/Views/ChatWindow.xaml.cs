using System.Windows;

namespace MVXTester.Chat.Views;

public partial class ChatWindow : Window
{
    public ChatWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 닫기 대신 숨기기 (ViewModel 유지)
        e.Cancel = true;
        Hide();
    }
}
