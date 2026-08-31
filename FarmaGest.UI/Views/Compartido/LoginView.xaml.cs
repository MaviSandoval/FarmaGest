using System.Windows;
using System.Windows.Controls;
using FarmaGest.UI.ViewModels.Compartido;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI.Views.Compartido;

public partial class LoginView : Page
{
    private readonly LoginViewModel _viewModel;

    public LoginView()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel();
        DataContext = _viewModel;

        // Suscribimos la navegación cuando el ViewModel autoriza el ingreso
        _viewModel.OnLoginExitoso = NavegarSegunPerfil;
    }

    private void TxtPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Contrasena = TxtPassword.Password;
        }
    }

    private void NavegarSegunPerfil(string perfil)
    {
        switch (perfil)
        {
            case "Administrador":
                var mainWindow = App.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                break;

            default:
                // Todavía no hay vistas armadas para Farmaceutico/Cliente
                MessageBox.Show(
                    $"El perfil '{perfil}' todavía no tiene una vista implementada.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return; // no cerramos el login si no hay a dónde navegar
        }

        // Cerramos la ventana de login (AuthWindow) una vez que se abrió el destino
        Window.GetWindow(this)?.Close();
    }
}