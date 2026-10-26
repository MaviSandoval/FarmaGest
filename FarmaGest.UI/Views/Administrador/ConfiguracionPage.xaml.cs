using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Datos.Repositorios;
using FarmaGest.Negocio.Servicios;
using Microsoft.Win32;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>
/// Configuración y copias de seguridad.
/// - Datos de la farmacia y parámetros: maqueta (todavía no hay tabla de configuración).
/// - Copia de seguridad: real, con BACKUP DATABASE / RESTORE DATABASE de SQL Server.
///   Manual (botón) + automática diaria a las 00:00 (BackupProgramadoService).
/// </summary>
public partial class ConfiguracionPage : Page
{
    private readonly BackupService _backupService;
    private readonly SesionUsuarioService _sesion;
    private readonly BackupProgramadoService _backupProgramado;

    // Evita guardar el interruptor mientras se carga su valor inicial
    private bool _cargandoInterruptor;

    public ConfiguracionPage(BackupService backupService, SesionUsuarioService sesion,
        BackupProgramadoService backupProgramado)
    {
        InitializeComponent();
        _backupService = backupService;
        _sesion = sesion;
        _backupProgramado = backupProgramado;
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CarpetaBackupText.Text))
                CarpetaBackupText.Text = await _backupService.ObtenerCarpetaAsync() ?? string.Empty;

            BackupsGrid.ItemsSource = await _backupService.ObtenerHistorialAsync();

            _cargandoInterruptor = true;
            BackupAutomaticoSwitch.IsChecked = await _backupService.AutomaticoActivoAsync();
            _cargandoInterruptor = false;
            MostrarEstadoAutomatico();

            if (_backupProgramado.UltimoError != null)
                Aviso($"La última copia automática falló:\n\n{_backupProgramado.UltimoError}\n\n" +
                      "Podés generar una copia manual mientras tanto.", MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Aviso($"No se pudo leer el historial de copias.\n\n{ex.Message}", MessageBoxImage.Warning);
        }
    }

    // =========================================================
    // BACKUP AUTOMÁTICO: activar / desactivar
    // =========================================================
    private async void BackupAutomatico_Changed(object sender, RoutedEventArgs e)
    {
        if (_cargandoInterruptor)
            return;

        bool activo = BackupAutomaticoSwitch.IsChecked == true;

        try
        {
            await _backupService.CambiarAutomaticoAsync(activo);
            MostrarEstadoAutomatico();
        }
        catch (Exception ex)
        {
            // No se pudo guardar: el interruptor vuelve a como estaba
            _cargandoInterruptor = true;
            BackupAutomaticoSwitch.IsChecked = !activo;
            _cargandoInterruptor = false;
            Aviso($"No se pudo guardar la configuración del backup automático.\n\n{ex.Message}", MessageBoxImage.Warning);
        }
    }

    private void MostrarEstadoAutomatico() =>
        BackupAutomaticoEstadoText.Text = BackupAutomaticoSwitch.IsChecked == true
            ? "Activado: todos los días a las 00:00 (o al abrir FarmaGest si a esa hora estaba cerrado) se guarda una copia en la carpeta de destino de abajo. Se conservan las últimas 7."
            : "Desactivado: solo se hacen copias manuales con \"Generar copia ahora\".";

    private void Guardar_Click(object sender, RoutedEventArgs e) =>
        Aviso("Configuración guardada (maqueta: todavía no hay tabla de configuración en la base).");

    private async void ElegirCarpeta_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { Title = "Carpeta donde guardar las copias de seguridad" };

        if (dialogo.ShowDialog() != true)
            return;

        CarpetaBackupText.Text = dialogo.FolderName;

        try
        {
            // Se guarda para las próximas copias, incluida la automática de las 00:00
            await _backupService.GuardarCarpetaAsync(dialogo.FolderName);
        }
        catch (Exception ex)
        {
            Aviso($"No se pudo guardar la carpeta elegida.\n\n{ex.Message}", MessageBoxImage.Warning);
        }
    }

    private async void GenerarBackup_Click(object sender, RoutedEventArgs e)
    {
        var usuario = _sesion.UsuarioActual;
        string generadoPor = usuario == null ? "Administrador" : $"{usuario.Nombre} {usuario.Apellido}";

        try
        {
            CursorOcupado(true);
            string archivo = await _backupService.GenerarBackupAsync(CarpetaBackupText.Text, generadoPor);
            await CargarAsync();
            Aviso($"Copia de seguridad generada:\n{archivo}");
        }
        catch (Exception ex)
        {
            Aviso($"No se pudo generar la copia.\n\n{ex.Message}\n\n" +
                  "Si el error es de acceso, elegí una carpeta en la que el servicio de SQL Server pueda escribir.",
                  MessageBoxImage.Warning);
        }
        finally
        {
            CursorOcupado(false);
        }
    }

    private async void RestaurarBackup_Click(object sender, RoutedEventArgs e)
    {
        string? archivo = (BackupsGrid.SelectedItem as BackupInfo)?.Archivo;

        if (archivo == null)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Elegí la copia de seguridad a restaurar",
                Filter = "Copia de seguridad (*.bak)|*.bak"
            };

            if (dialogo.ShowDialog() != true)
                return;

            archivo = dialogo.FileName;
        }

        var respuesta = MessageBox.Show(
            $"Se va a restaurar la base con la copia:\n{archivo}\n\n" +
            "Todos los datos cargados después de esa copia se van a perder y se cierran las conexiones abiertas.\n\n¿Continuar?",
            "Restaurar copia de seguridad", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            CursorOcupado(true);
            await _backupService.RestaurarBackupAsync(archivo);
            CursorOcupado(false);

            Aviso("La base se restauró correctamente. FarmaGest se va a cerrar: volvé a abrirlo para seguir trabajando.");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            CursorOcupado(false);
            Aviso($"No se pudo restaurar la copia.\n\n{ex.Message}", MessageBoxImage.Warning);
        }
    }

    private static void CursorOcupado(bool ocupado) =>
        System.Windows.Input.Mouse.OverrideCursor = ocupado ? System.Windows.Input.Cursors.Wait : null;

    private static void Aviso(string mensaje, MessageBoxImage icono = MessageBoxImage.Information) =>
        MessageBox.Show(mensaje, "FarmaGest", MessageBoxButton.OK, icono);
}
