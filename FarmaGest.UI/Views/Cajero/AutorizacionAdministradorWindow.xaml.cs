using System;
using System.Windows;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Cajero;

public partial class AutorizacionAdministradorWindow : Window
{
    private readonly UsuarioService _usuarioService;

    public AutorizacionAdministradorWindow(
        UsuarioService usuarioService)
    {
        InitializeComponent();

        _usuarioService = usuarioService;

        Loaded += (_, _) => TxtUsuario.Focus();
    }

    private async void Autorizar_Click(
        object sender,
        RoutedEventArgs e)
    {
        TxtMensaje.Text = string.Empty;

        var usuarioODni = TxtUsuario.Text.Trim();
        var contrasena = TxtContrasena.Password;

        if (string.IsNullOrWhiteSpace(usuarioODni) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            TxtMensaje.Text =
                "Ingrese el usuario/DNI y la contraseña.";

            return;
        }

        try
        {
            BtnAutorizar.IsEnabled = false;

            var usuario =
                await _usuarioService.ValidarCredencialesAsync(
                    usuarioODni,
                    contrasena);

            if (usuario == null)
            {
                TxtMensaje.Text =
                    "Usuario o contraseña incorrectos.";

                return;
            }

            if (usuario.Rol == null ||
                !string.Equals(
                    usuario.Rol.Nombre,
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                TxtMensaje.Text =
                    "El usuario ingresado no tiene permisos de administrador.";

                return;
            }

            // Autorización correcta.
            // NO modificamos la sesión actual del cajero.
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            TxtMensaje.Text =
                "No se pudo validar la autorización: " +
                ex.Message;
        }
        finally
        {
            BtnAutorizar.IsEnabled = true;
        }
    }

    private void Cancelar_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}