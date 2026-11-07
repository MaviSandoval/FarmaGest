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
    private int _versionApertura;

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
        if (e.Key == Key.Down && _viewModel.UsuariosRecordados.Count > 0)
        {
            UsuariosPopup.IsOpen = true;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape)
        {
            CerrarSugerencias();
            return;
        }
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

    private void TxtUsuario_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Abrir después del clic evita que la captura del Popup interprete
        // ese mismo clic sobre el campo como un clic exterior.
        var version = ++_versionApertura;
        Dispatcher.InvokeAsync(() =>
        {
            if (version == _versionApertura && TxtUsuario.IsKeyboardFocusWithin)
                UsuariosPopup.IsOpen = _viewModel.UsuariosRecordados.Count > 0;
        },
            System.Windows.Threading.DispatcherPriority.Input);
    }

    private void CerrarSugerencias()
    {
        // Invalidar también una apertura diferida que todavía no se ejecutó.
        _versionApertura++;
        UsuariosPopup.IsOpen = false;
    }

    private void Login_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!TxtUsuario.IsMouseOver && UsuariosPopup.Child?.IsMouseOver != true)
            CerrarSugerencias();
    }

    private void TxtPassword_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        => CerrarSugerencias();

    private void TxtPassword_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => CerrarSugerencias();

    private void Login_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Esperar al nuevo foco permite interactuar con las opciones y con ×.
        Dispatcher.InvokeAsync(() =>
        {
            if (!TxtUsuario.IsKeyboardFocusWithin && UsuariosPopup.Child?.IsKeyboardFocusWithin != true)
                CerrarSugerencias();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SeleccionarUsuario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string identificador })
        {
            _viewModel.Usuario = identificador;
            CerrarSugerencias();
            TxtPassword.Focus();
        }
    }

    private void EliminarUsuario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string identificador })
        {
            _viewModel.EliminarUsuarioRecordado(identificador);
            if (_viewModel.UsuariosRecordados.Count == 0) CerrarSugerencias();
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
