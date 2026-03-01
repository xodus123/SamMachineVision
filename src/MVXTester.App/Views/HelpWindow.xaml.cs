using System.Windows;

namespace MVXTester.App.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
