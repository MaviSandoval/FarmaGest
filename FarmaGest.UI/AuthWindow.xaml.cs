using FarmaGest.UI.Views.Compartido;

namespace FarmaGest.UI;

public partial class AuthWindow : Wpf.Ui.Controls.FluentWindow
{
    public AuthWindow()
    {
        InitializeComponent();
        AuthFrame.Navigate(new LoginView());
    }
}