using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FarmaGest.UI.ViewModels.Compartido;
using Microsoft.Extensions.DependencyInjection;

using FarmaGest.UI.Views.Farmaceutico;
using FarmaGest.UI.Views.Cajero;

namespace FarmaGest.UI.Views.Compartido;

public partial class LoginView : Page
{
    private readonly LoginViewModel _viewModel;

    public LoginView()
    {
        InitializeComponent();

        _viewModel =
            App.Services.GetRequiredService<LoginViewModel>();

        DataContext = _viewModel;

        // Suscribimos la navegación cuando
        // el ViewModel autoriza el ingreso.
        _viewModel.OnLoginExitoso =
            NavegarSegunPerfil;

        _viewModel.OnRequiereCambioContrasena =
            NavegarACambioContrasena;
    }

    // =====================================================
    // PERMITIR PEGAR USUARIO CON CTRL + V
    // =====================================================

    private void TxtUsuario_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.V &&
            Keyboard.Modifiers.HasFlag(
                ModifierKeys.Control))
        {
            if (Clipboard.ContainsText())
            {
                TxtUsuario.Text =
                    Clipboard.GetText().Trim();

                TxtUsuario.CaretIndex =
                    TxtUsuario.Text.Length;

                // Actualizar también el ViewModel
                _viewModel.Usuario =
                    TxtUsuario.Text;

                e.Handled = true;
            }
        }
    }

    // =====================================================
    // ACTUALIZAR CONTRASEÑA EN VIEWMODEL
    // =====================================================

    private void TxtPassword_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Contrasena =
                TxtPassword.Password;
        }
    }

    // =====================================================
    // PERMITIR PEGAR CONTRASEÑA CON CTRL + V
    // =====================================================

    private void TxtPassword_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.V &&
            Keyboard.Modifiers.HasFlag(
                ModifierKeys.Control))
        {
            if (Clipboard.ContainsText())
            {
                TxtPassword.Password =
                    Clipboard.GetText();

                TxtPassword.CaretIndex =
                    TxtPassword.Password.Length;

                e.Handled = true;
            }
        }
    }

    // =====================================================
    // NAVEGAR SEGÚN PERFIL
    // =====================================================

    private void NavegarSegunPerfil(string perfil)
    {
        switch (perfil)
        {
            case "Administrador":

                var mainWindow =
                    App.Services
                        .GetRequiredService<MainWindow>();

                mainWindow.Show();

                break;

            case "Farmaceutico":

                var mainWindowFarmaceutico =
                    App.Services
                        .GetRequiredService<
                            MainWindowFarmaceutico>();

                mainWindowFarmaceutico.Show();

                break;

            case "Cajero":

                var mainWindowCajero =
                    App.Services
                        .GetRequiredService<
                            MainWindowCajero>();

                mainWindowCajero.Show();

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

    // =====================================================
    // CAMBIO OBLIGATORIO DE CONTRASEÑA
    // =====================================================

    private void NavegarACambioContrasena(
        FarmaGest.Dominio.Usuario usuario)
    {
        NavigationService?.Navigate(
            new CambiarContrasenaView(usuario));
    }
}