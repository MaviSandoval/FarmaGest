using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;
using Microsoft.Win32;

namespace FarmaGest.UI.Views.Compartido;

/// <summary>
/// Mi perfil: foto, datos personales y cambio de contraseña del usuario logueado.
/// Todos los cambios se guardan con sp_Usuario_Modificacion (a través de UsuarioService).
/// </summary>
public partial class MiPerfilPage : Page
{
    private readonly UsuarioService _usuarioService;
    private readonly SesionUsuarioService _sesionUsuarioService;
    private Usuario? _usuario;

    public MiPerfilPage(UsuarioService usuarioService, SesionUsuarioService sesionUsuarioService)
    {
        InitializeComponent();

        _usuarioService = usuarioService;
        _sesionUsuarioService = sesionUsuarioService;

        Loaded += async (_, _) => await CargarUsuarioAsync();
    }

    // =========================================================
    // CARGA
    // =========================================================
    private async Task CargarUsuarioAsync()
    {
        var idUsuario = _sesionUsuarioService.UsuarioActual?.Id;
        if (idUsuario == null)
            return;

        _usuario = await _usuarioService.ObtenerPorIdAsync(idUsuario.Value);
        if (_usuario == null)
            return;

        MostrarUsuario();
    }

    private void MostrarUsuario()
    {
        if (_usuario == null)
            return;

        AvatarGrande.Mostrar(_usuario);
        NombreCompletoText.Text = $"{_usuario.Nombre} {_usuario.Apellido}";
        RolText.Text = NombreRolVisible(_usuario.Rol?.Nombre);
        QuitarFotoButton.Visibility = _usuario.Foto != null ? Visibility.Visible : Visibility.Collapsed;

        NombreText.Text = _usuario.Nombre;
        ApellidoText.Text = _usuario.Apellido;
        EmailText.Text = _usuario.Email ?? string.Empty;
        DniText.Text = _usuario.Dni;
    }

    /// <summary>Vuelve a leer el usuario de la base y avisa a la ventana principal (encabezado).</summary>
    private async Task RefrescarAsync()
    {
        if (_usuario == null)
            return;

        var actualizado = await _usuarioService.ObtenerPorIdAsync(_usuario.Id);
        if (actualizado == null)
            return;

        _usuario = actualizado;
        _sesionUsuarioService.ActualizarUsuario(actualizado);
        MostrarUsuario();
    }

    // =========================================================
    // FOTO
    // =========================================================
    private async void CambiarFoto_Click(object sender, RoutedEventArgs e)
    {
        if (_usuario == null)
            return;

        var dialogo = new OpenFileDialog
        {
            Title = "Elegir foto de perfil",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialogo.ShowDialog() != true)
            return;

        try
        {
            var foto = ImagenHelper.CargarYReducir(dialogo.FileName);
            await _usuarioService.ActualizarFotoAsync(_usuario.Id, foto);
            await RefrescarAsync();
            MostrarMensaje(MensajeFotoText, "Foto actualizada.", true);
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensaje(MensajeFotoText, ex.Message, false);
        }
        catch (Exception)
        {
            MostrarMensaje(MensajeFotoText, "No se pudo cargar la imagen. Probá con otro archivo.", false);
        }
    }

    private async void QuitarFoto_Click(object sender, RoutedEventArgs e)
    {
        if (_usuario == null)
            return;

        try
        {
            await _usuarioService.ActualizarFotoAsync(_usuario.Id, null);
            await RefrescarAsync();
            MostrarMensaje(MensajeFotoText, "Foto eliminada.", true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(MensajeFotoText, ex.Message, false);
        }
    }

    // =========================================================
    // DATOS PERSONALES
    // =========================================================
    private async void GuardarDatos_Click(object sender, RoutedEventArgs e)
    {
        if (_usuario == null)
            return;

        try
        {
            await _usuarioService.ActualizarDatosPerfilAsync(
                _usuario.Id, NombreText.Text, ApellidoText.Text, EmailText.Text);

            await RefrescarAsync();
            MostrarMensaje(MensajeDatosText, "Datos actualizados correctamente.", true);
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensaje(MensajeDatosText, ex.Message, false);
        }
        catch (Exception)
        {
            MostrarMensaje(MensajeDatosText, "Ocurrió un error al guardar. Intentá nuevamente.", false);
        }
    }

    // =========================================================
    // CONTRASEÑA
    // =========================================================
    private async void CambiarContrasena_Click(object sender, RoutedEventArgs e)
    {
        if (_usuario == null)
            return;

        if (string.IsNullOrWhiteSpace(ContrasenaActualBox.Password) ||
            string.IsNullOrWhiteSpace(NuevaContrasenaBox.Password))
        {
            MostrarMensaje(MensajeContrasenaText, "Completá la contraseña actual y la nueva.", false);
            return;
        }

        if (NuevaContrasenaBox.Password != ConfirmarContrasenaBox.Password)
        {
            MostrarMensaje(MensajeContrasenaText, "Las contraseñas nuevas no coinciden.", false);
            return;
        }

        try
        {
            await _usuarioService.CambiarContrasenaPerfilAsync(
                _usuario.Id, ContrasenaActualBox.Password, NuevaContrasenaBox.Password);

            ContrasenaActualBox.Clear();
            NuevaContrasenaBox.Clear();
            ConfirmarContrasenaBox.Clear();

            MostrarMensaje(MensajeContrasenaText, "Contraseña actualizada.", true);
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensaje(MensajeContrasenaText, ex.Message, false);
        }
        catch (Exception)
        {
            MostrarMensaje(MensajeContrasenaText, "Ocurrió un error al cambiar la contraseña.", false);
        }
    }

    // =========================================================
    // AUXILIARES
    // =========================================================
    private void MostrarMensaje(TextBlock destino, string texto, bool exito)
    {
        destino.Text = texto;
        destino.Foreground = (Brush)FindResource(exito ? "FarmaSuccessBrush" : "FarmaDangerBrush");
    }

    private static string NombreRolVisible(string? rol) => rol switch
    {
        "Farmaceutico" => "Farmacéutico",
        null => string.Empty,
        _ => rol
    };
}
