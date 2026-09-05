using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.UI.ViewModels.Compartido;
using Microsoft.Extensions.DependencyInjection;
using FarmaGest.UI.Views.Farmaceutico;

namespace FarmaGest.UI.Views.Compartido;

public partial class CambiarContrasenaView : Page
{
    private readonly CambiarContrasenaViewModel _viewModel;

    public CambiarContrasenaView(Usuario usuario)
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<CambiarContrasenaViewModel>();
        _viewModel.Inicializar(usuario);
        DataContext = _viewModel;

        _viewModel.OnCambioExitoso = NavegarSegunPerfil;
    }

    private void TxtNuevaContrasena_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is CambiarContrasenaViewModel vm)
            vm.NuevaContrasena = TxtNuevaContrasena.Password;
    }

    private void TxtConfirmarContrasena_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is CambiarContrasenaViewModel vm)
            vm.ConfirmarContrasena = TxtConfirmarContrasena.Password;
    }

    private void NavegarSegunPerfil(string perfil)
    {
        switch (perfil)
        {
            case "Administrador":
                var mainWindow = App.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                break;

            case "Farmaceutico":
                var mainWindowFarmaceutico = App.Services.GetRequiredService<MainWindowFarmaceutico>();
                mainWindowFarmaceutico.Show();
                break;

            default:
                MessageBox.Show(
                    $"El perfil '{perfil}' todavía no tiene una vista implementada.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
        }

        Window.GetWindow(this)?.Close();
    }
}