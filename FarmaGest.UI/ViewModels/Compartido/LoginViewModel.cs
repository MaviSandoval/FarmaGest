using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using FarmaGest.UI.Servicios;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.ViewModels.Compartido;

public partial class LoginViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;
    private readonly SesionUsuarioService _sesionUsuarioService;
    private readonly UsuariosRecordadosService _usuariosRecordadosService;
    public ObservableCollection<string> UsuariosRecordados { get; } = new();

    [ObservableProperty]
    private string _mensajeHistorial = string.Empty;
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

    public LoginViewModel(
    UsuarioService usuarioService,
    SesionUsuarioService sesionUsuarioService,
    UsuariosRecordadosService usuariosRecordadosService)
    {
        _usuarioService = usuarioService;
        _sesionUsuarioService = sesionUsuarioService;
        _usuariosRecordadosService = usuariosRecordadosService;
        foreach (var identificador in _usuariosRecordadosService.Leer())
            UsuariosRecordados.Add(identificador);
    }

    public void EliminarUsuarioRecordado(string identificador)
    {
        var restantes = UsuariosRecordados.Where(u =>
            !string.Equals(u, identificador, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!_usuariosRecordadosService.Guardar(restantes))
        {
            MensajeHistorial = "No se pudo eliminar el usuario del historial local.";
            return;
        }
        MensajeHistorial = string.Empty;
        UsuariosRecordados.Remove(identificador);
    }

    private void RecordarUsuario(Usuario autenticado)
    {
        var identificador = (string.IsNullOrWhiteSpace(autenticado.Email)
            ? autenticado.Dni : autenticado.Email).Trim();
        var anteriores = UsuariosRecordados.Where(u =>
            !string.Equals(u, identificador, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(u, autenticado.Dni, StringComparison.OrdinalIgnoreCase)).ToList();
        anteriores.Insert(0, identificador);
        // El historial es opcional: un error de disco no modifica el resultado del login.
        if (!_usuariosRecordadosService.Guardar(anteriores)) return;
        UsuariosRecordados.Clear();
        foreach (var anterior in anteriores) UsuariosRecordados.Add(anterior);
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
        RecordarUsuario(usuarioAutenticado);

        // Guardamos el usuario autenticado en la sesión
        _sesionUsuarioService.IniciarSesion(usuarioAutenticado);

        if (usuarioAutenticado.RequiereCambioContrasena)
        {
            OnRequiereCambioContrasena?.Invoke(usuarioAutenticado);
            return;
        }

        // Notificamos a la vista pasándole el nombre del Rol
        OnLoginExitoso?.Invoke(usuarioAutenticado.Rol.Nombre);
    }
}
