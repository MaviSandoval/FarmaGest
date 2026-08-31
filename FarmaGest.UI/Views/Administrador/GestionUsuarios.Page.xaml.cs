using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Administrador;

public partial class GestionUsuariosPage : Page
{
    private readonly GestionUsuariosService _service;
    private int? _idEnEdicion;

    public GestionUsuariosPage(GestionUsuariosService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        RolCombo.ItemsSource = await _service.ObtenerRolesAsync();

        var usuarios = await _service.ObtenerTodosAsync();
        UsuariosGrid.ItemsSource = usuarios.Select(u => new
        {
            Usuario = u,
            u.Dni,
            u.Nombre,
            u.Apellido,
            u.Email,
            u.Rol,
            EstadoTexto = u.Estado ? "Activo" : "Inactivo",
            TextoAccionEstado = u.Estado ? "Dar de baja" : "Reactivar"
        }).ToList();
    }

    private async void GuardarButton_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(DniText.Text) ||
            string.IsNullOrWhiteSpace(NombreText.Text) ||
            string.IsNullOrWhiteSpace(ApellidoText.Text) ||
            RolCombo.SelectedValue is null)
        {
            MensajeText.Text = "Completá DNI, Nombre, Apellido y Rol.";
            return;
        }

        var rolId = ((Rol)RolCombo.SelectedItem).Id;
        var email = string.IsNullOrWhiteSpace(EmailText.Text) ? null : EmailText.Text;

        try
        {
            if (_idEnEdicion is null)
            {
                var contrasenaTemporal = await _service.CrearAsync(
                    DniText.Text, NombreText.Text, ApellidoText.Text, email, rolId);

                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Usuario creado",
                    Content = $"Contraseña temporal: {contrasenaTemporal}\n\n" +
                "Copiala ahora — no se va a volver a mostrar. " +
                "El usuario deberá cambiarla en su primer ingreso."
                }.ShowDialogAsync();
            }
            else
            {
                await _service.EditarAsync(
                    _idEnEdicion.Value, DniText.Text, NombreText.Text, ApellidoText.Text, email, rolId);
            }

            LimpiarFormulario();
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MensajeText.Text = $"Error al guardar: {ex.Message}";
        }
    }

    private void EditarUsuario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not { } item) return;

        var usuario = (Usuario)item.GetType().GetProperty("Usuario")!.GetValue(item)!;

        _idEnEdicion = usuario.Id;
        TituloFormularioText.Text = $"Editando: {usuario.Nombre} {usuario.Apellido}";
        DniText.Text = usuario.Dni;
        NombreText.Text = usuario.Nombre;
        ApellidoText.Text = usuario.Apellido;
        EmailText.Text = usuario.Email;
        RolCombo.SelectedValue = usuario.RolId;
        CancelarButton.Visibility = Visibility.Visible;
    }

    private async void CambiarEstadoUsuario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not { } item) return;

        var usuario = (Usuario)item.GetType().GetProperty("Usuario")!.GetValue(item)!;
        await _service.CambiarEstadoAsync(usuario.Id, !usuario.Estado);
        await CargarDatosAsync();
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

    private void LimpiarFormulario()
    {
        _idEnEdicion = null;
        TituloFormularioText.Text = "Nuevo usuario";
        DniText.Text = NombreText.Text = ApellidoText.Text = EmailText.Text = string.Empty;
        RolCombo.SelectedItem = null;
        CancelarButton.Visibility = Visibility.Collapsed;
        MensajeText.Text = string.Empty;
    }
}