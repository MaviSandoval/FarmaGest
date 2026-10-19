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

        Loaded += async (_, _) =>
            await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        RolCombo.ItemsSource =
            await _service.ObtenerRolesAsync();

        var usuarios =
            await _service.ObtenerTodosAsync();

        UsuariosGrid.ItemsSource = usuarios.Select(u => new
        {
            Usuario = u,
            u.Dni,
            u.Nombre,
            u.Apellido,
            u.Email,
            u.Rol,
            EstadoTexto =
                u.Estado ? "Activo" : "Inactivo",
            TextoAccionEstado =
                u.Estado ? "Dar de baja" : "Reactivar"
        }).ToList();
    }

    private async void GuardarButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        // Validación rápida de campos obligatorios
        if (string.IsNullOrWhiteSpace(DniText.Text) ||
            string.IsNullOrWhiteSpace(NombreText.Text) ||
            string.IsNullOrWhiteSpace(ApellidoText.Text) ||
            RolCombo.SelectedValue is null)
        {
            MensajeText.Text =
                "Completá DNI, Nombre, Apellido y Rol.";

            return;
        }

        // Cantidad de caracteres: el DNI tiene 7 u 8 dígitos
        if (DniText.Text.Trim().Length is < 7 or > 8)
        {
            MensajeText.Text =
                "El DNI debe tener 7 u 8 dígitos.";

            return;
        }

        var rolId =
            ((Rol)RolCombo.SelectedItem).Id;

        var email =
            string.IsNullOrWhiteSpace(EmailText.Text)
                ? null
                : EmailText.Text;

        try
        {
            // =============================================
            // CREAR USUARIO
            // =============================================
            if (_idEnEdicion is null)
            {
                var contrasenaTemporal =
                    await _service.CrearAsync(
                        DniText.Text,
                        NombreText.Text,
                        ApellidoText.Text,
                        email,
                        rolId);

                await new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Usuario creado",

                    Content =
                        $"Contraseña temporal: {contrasenaTemporal}\n\n" +
                        "Copiala ahora — no se va a volver a mostrar. " +
                        "El usuario deberá cambiarla en su primer ingreso."

                }.ShowDialogAsync();
            }

            // =============================================
            // EDITAR USUARIO
            // =============================================
            else
            {
                await _service.EditarAsync(
                    _idEnEdicion.Value,
                    DniText.Text,
                    NombreText.Text,
                    ApellidoText.Text,
                    email,
                    rolId);
            }

            LimpiarFormulario();

            await CargarDatosAsync();
        }

        // =============================================
        // ERRORES DE VALIDACIÓN
        // =============================================
        catch (InvalidOperationException ex)
        {
            MensajeText.Text = ex.Message;
        }

        // =============================================
        // ERROR INESPERADO
        // =============================================
        catch (Exception)
        {
            MensajeText.Text =
                "Ocurrió un error al guardar el usuario. Intentá nuevamente.";
        }
    }

    private void EditarUsuario_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe ||
            fe.Tag is not { } item)
        {
            return;
        }

        var usuario =
            (Usuario)item
                .GetType()
                .GetProperty("Usuario")!
                .GetValue(item)!;

        _idEnEdicion = usuario.Id;

        TituloFormularioText.Text =
            $"Editando: {usuario.Nombre} {usuario.Apellido}";

        DniText.Text = usuario.Dni;
        NombreText.Text = usuario.Nombre;
        ApellidoText.Text = usuario.Apellido;
        EmailText.Text = usuario.Email;

        RolCombo.SelectedValue =
            usuario.RolId;

        CancelarButton.Visibility =
            Visibility.Visible;
    }

    private async void CambiarEstadoUsuario_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe ||
            fe.Tag is not { } item)
        {
            return;
        }

        var usuario =
            (Usuario)item
                .GetType()
                .GetProperty("Usuario")!
                .GetValue(item)!;

        // Confirmación: se muestran los datos del usuario y el foco queda en "No"
        string accion = usuario.Estado ? "dar de baja" : "reactivar";

        var respuesta = System.Windows.MessageBox.Show(
            $"¿Seguro que querés {accion} a este usuario?\n\n" +
            $"Nombre:  {usuario.Nombre} {usuario.Apellido}\n" +
            $"DNI:  {usuario.Dni}\n" +
            $"Email:  {usuario.Email ?? "—"}\n" +
            $"Rol:  {usuario.Rol?.Nombre ?? "—"}\n" +
            $"Estado actual:  {(usuario.Estado ? "Activo" : "Inactivo")}\n\n" +
            (usuario.Estado
                ? "Es una baja lógica: el usuario no se borra, pero ya no va a poder ingresar al sistema."
                : "El usuario va a poder ingresar de nuevo al sistema."),
            usuario.Estado ? "Confirmar baja de usuario" : "Confirmar reactivación de usuario",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);

        if (respuesta != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            // Baja lógica / reactivación con sp_Usuario_Modificacion
            await _service.CambiarEstadoAsync(
                usuario.Id,
                !usuario.Estado);

            MensajeText.Text = string.Empty;

            await CargarDatosAsync();
        }
        catch (InvalidOperationException ex)
        {
            // Ej.: "No se puede dar de baja ni cambiar el rol del último Administrador activo."
            MensajeText.Text = ex.Message;
        }
    }

    private void CancelarButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        _idEnEdicion = null;

        TituloFormularioText.Text =
            "Nuevo usuario";

        DniText.Text = string.Empty;
        NombreText.Text = string.Empty;
        ApellidoText.Text = string.Empty;
        EmailText.Text = string.Empty;

        RolCombo.SelectedItem = null;

        CancelarButton.Visibility =
            Visibility.Collapsed;

        MensajeText.Text =
            string.Empty;
    }
}