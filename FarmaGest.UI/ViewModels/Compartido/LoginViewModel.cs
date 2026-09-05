using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.ViewModels.Compartido;

public partial class LoginViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;
    private int _intentosFallidos = 0;
    private const int MaxIntentos = 3;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _contrasena = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    // Acción para notificar a la vista cuando el login es correcto
    public Action<string>? OnLoginExitoso;

    // Acción para notificar a la vista cuando hay que forzar el cambio de contraseña
    public Action<Usuario>? OnRequiereCambioContrasena;

    public LoginViewModel(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        MensajeError = string.Empty;

        if (_intentosFallidos >= MaxIntentos)
        {
            MensajeError = "Usuario bloqueado temporalmente por intentos fallidos.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError = "Debe completar usuario y contraseña.";
            return;
        }

        var usuarioAutenticado = await _usuarioService.ValidarCredencialesAsync(Usuario, Contrasena);

        if (usuarioAutenticado == null)
        {
            _intentosFallidos++;
            MensajeError = $"Usuario o contraseña incorrectos. (Intento {_intentosFallidos}/{MaxIntentos})";
            return;
        }

        _intentosFallidos = 0;

        if (usuarioAutenticado.RequiereCambioContrasena)
        {
            OnRequiereCambioContrasena?.Invoke(usuarioAutenticado);
            return;
        }

        // Notificamos a la vista pasándole el nombre del Rol
        OnLoginExitoso?.Invoke(usuarioAutenticado.Rol.Nombre);
    }
}