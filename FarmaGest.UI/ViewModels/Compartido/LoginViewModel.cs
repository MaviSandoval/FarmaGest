using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public LoginViewModel()
    {
        _usuarioService = new UsuarioService();
    }

    [RelayCommand]
    private void IniciarSesion()
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

        var usuarioAutenticado = _usuarioService.ValidarCredenciales(Usuario, Contrasena);

        if (usuarioAutenticado == null)
        {
            _intentosFallidos++;
            MensajeError = $"Usuario o contraseña incorrectos. (Intento {_intentosFallidos}/{MaxIntentos})";
            return;
        }

        _intentosFallidos = 0;

        // Notificamos a la vista pasándole el Perfil
        OnLoginExitoso?.Invoke(usuarioAutenticado.Perfil);
    }
}