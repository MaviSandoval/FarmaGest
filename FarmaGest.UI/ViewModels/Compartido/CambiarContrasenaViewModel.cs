using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.ViewModels.Compartido;

public partial class CambiarContrasenaViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;
    private Usuario? _usuario;

    [ObservableProperty]
    private string _nuevaContrasena = string.Empty;

    [ObservableProperty]
    private string _confirmarContrasena = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    public Action<string>? OnCambioExitoso;

    public CambiarContrasenaViewModel(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    public void Inicializar(Usuario usuario)
    {
        _usuario = usuario;
    }

    [RelayCommand]
    private async Task ConfirmarCambioAsync()
    {
        MensajeError = string.Empty;

        if (_usuario == null)
        {
            MensajeError = "No se encontró el usuario a actualizar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevaContrasena) || string.IsNullOrWhiteSpace(ConfirmarContrasena))
        {
            MensajeError = "Debe completar ambos campos.";
            return;
        }

        if (NuevaContrasena != ConfirmarContrasena)
        {
            MensajeError = "Las contraseñas no coinciden.";
            return;
        }

        if (NuevaContrasena.Length < 6)
        {
            MensajeError = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        var exito = await _usuarioService.CambiarContrasenaAsync(_usuario.Id, NuevaContrasena);

        if (!exito)
        {
            MensajeError = "No se pudo actualizar la contraseña. Intente nuevamente.";
            return;
        }

        OnCambioExitoso?.Invoke(_usuario.Rol.Nombre);
    }
}