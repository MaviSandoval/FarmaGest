using System.Windows.Controls;
using FarmaGest.UI.ViewModels.Compartido;

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
        // Navegación según perfil no implementada aún. Evitar referencias a vistas inexistentes
        // Implementar navegación real cuando las vistas de destino estén disponibles.
        return;
    }
}