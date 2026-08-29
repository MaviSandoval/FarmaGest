using System.Windows;
using FarmaGest.UI.Views.Compartido;
using Wpf.Ui.Controls;

namespace FarmaGest.UI;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new LoginView());
    }
}