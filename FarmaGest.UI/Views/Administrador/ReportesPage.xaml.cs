using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>
/// Reportes del Administrador (los de ventas son del Farmacéutico):
///  1. Copias de seguridad ..... msdb.dbo.backupset + backupmediafamily
///  2. Usuarios ................ Usuario + Rol (el admin gestiona usuarios; las ventas las ve el Farmacéutico)
///  3. Usuarios por rol ........ Usuario + Rol
/// </summary>
public partial class ReportesPage : Page
{
    private readonly ReporteSistemaService _reporteService;

    private ReporteBackupsDto? _backups;
    private List<UsuarioReporteDto> _usuarios = new();
    private List<UsuariosPorRolDto> _roles = new();

    public ReportesPage(ReporteSistemaService reporteService)
    {
        InitializeComponent();
        _reporteService = reporteService;

        DesdePicker.SelectedDate = DateTime.Today.AddDays(-29);
        HastaPicker.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await GenerarAsync();
        PreviewMouseWheel += ReenviarRueda;
    }

    private async Task GenerarAsync()
    {
        if (DesdePicker.SelectedDate is not DateTime desde || HastaPicker.SelectedDate is not DateTime hasta)
        {
            MessageBox.Show("Elegí las fechas Desde y Hasta.", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (desde > hasta)
        {
            MessageBox.Show("La fecha Desde no puede ser posterior a Hasta.", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _backups = await _reporteService.BackupsAsync(desde, hasta);
            _usuarios = await _reporteService.UsuariosAsync();
            _roles = await _reporteService.UsuariosPorRolAsync();

            BackupsGrid.ItemsSource = _backups.Copias;
            BackupsVacioText.Visibility = _backups.Copias.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UsuariosGrid.ItemsSource = _usuarios;
            RolesGrid.ItemsSource = _roles;

            ActualizarIndicadores(desde, hasta);
            ActualizarGraficos(desde, hasta);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron generar los reportes.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // =========================================================
    // INDICADORES
    // =========================================================
    private void ActualizarIndicadores(DateTime desde, DateTime hasta)
    {
        var b = _backups!;
        var verde = (Brush)FindResource("FarmaPrimaryBrush");
        var rojo = (Brush)FindResource("FarmaDangerBrush");

        if (b.Ultima is { } ultima)
        {
            int dias = (DateTime.Today - ultima.Fecha.Date).Days;
            UltimaCopiaText.Text = ultima.Fecha.ToString("dd/MM HH:mm");
            UltimaCopiaText.Foreground = dias > 1 ? rojo : verde;
            UltimaCopiaDetalleText.Text = dias switch
            {
                0 => $"Hoy · {ultima.Tamano}",
                1 => $"Ayer · {ultima.Tamano}",
                _ => $"Hace {dias} días · conviene generar una"
            };
        }
        else
        {
            UltimaCopiaText.Text = "Nunca";
            UltimaCopiaText.Foreground = rojo;
            UltimaCopiaDetalleText.Text = "Todavía no se hizo ninguna copia";
        }

        CopiasText.Text = b.Copias.Count.ToString();
        CopiasDetalleText.Text = $"{b.Automaticas} automática(s) · {b.Manuales} manual(es) · {b.DiasSinCopia} día(s) sin copia";

        AutomaticoText.Text = b.AutomaticoActivo ? "Activado" : "Desactivado";
        AutomaticoText.Foreground = b.AutomaticoActivo ? verde : rojo;
        AutomaticoDetalleText.Text = b.AutomaticoActivo ? "Todos los días a las 00:00" : "Se cambia en Configuración";

        int activos = _roles.Sum(r => r.Activos);
        UsuariosText.Text = activos.ToString();
        UsuariosDetalleText.Text = $"de {_roles.Sum(r => r.Total)} registrados · {_roles.Sum(r => r.CambioPendiente)} deben cambiar la contraseña";
    }

    // =========================================================
    // GRÁFICOS
    // =========================================================
    private void ActualizarGraficos(DateTime desde, DateTime hasta)
    {
        // Copias por día: un punto por cada día del período (o por semana si es largo)
        var copias = _backups!.Copias;
        var dias = Enumerable.Range(0, (hasta.Date - desde.Date).Days + 1).Select(d => desde.Date.AddDays(d)).ToList();
        bool porSemana = dias.Count > 31;

        var grupos = porSemana
            ? dias.GroupBy(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))).Select(g => (Etiqueta: g.Key.ToString("dd/MM"), Dias: g.ToList())).ToList()
            : dias.Select(d => (Etiqueta: d.ToString("dd/MM"), Dias: new List<DateTime> { d })).ToList();

        Graficos.BarrasApiladas(BackupsChart, grupos.Select(g => g.Etiqueta), Graficos.Entero,
            ("Automáticas", grupos.Select(g => (double)copias.Count(c => c.Tipo == "Automático" && g.Dias.Contains(c.Fecha.Date))), Graficos.Verde),
            ("Manuales", grupos.Select(g => (double)copias.Count(c => c.Tipo == "Manual" && g.Dias.Contains(c.Fecha.Date))), Graficos.Celeste));
        Graficos.Mostrar(BackupsChartCard, copias.Count > 0);

        Graficos.Dona(RolesChart, _roles.Select(r => (r.Rol, (double)r.Activos)));
        Graficos.Mostrar(RolesChartCard, _roles.Any(r => r.Activos > 0));
    }

    private async void Generar_Click(object sender, RoutedEventArgs e) => await GenerarAsync();

    private async void Rapido_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int dias))
        {
            HastaPicker.SelectedDate = DateTime.Today;
            DesdePicker.SelectedDate = DateTime.Today.AddDays(-(dias - 1));
            await GenerarAsync();
        }
    }

    // Las tablas y los gráficos se quedan con la rueda del mouse: se reenvía al scroll de la página
    private void ReenviarRueda(object sender, MouseWheelEventArgs e)
    {
        ScrollPrincipal.ScrollToVerticalOffset(ScrollPrincipal.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    // =========================================================
    // EXPORTACIÓN
    // =========================================================
    private string Periodo => $"Período: {DesdePicker.SelectedDate:dd/MM/yyyy} al {HastaPicker.SelectedDate:dd/MM/yyyy}";

    private SeccionPdf SeccionBackups() => new()
    {
        Titulo = "Copias de seguridad",
        Columnas = new[] { "Fecha", "Tipo", "Generada por", "Archivo", "Tamaño (MB)", "Archivo .bak" },
        Filas = (_backups?.Copias ?? new()).Select(c => new object?[] { c.FechaTexto, c.Tipo, c.GeneradaPor, c.Archivo, (double)c.TamanoMb, c.EstadoTexto }).ToList()
    };

    private SeccionPdf SeccionUsuarios() => new()
    {
        Titulo = "Usuarios",
        Columnas = new[] { "Usuario", "DNI", "Email", "Rol", "Estado", "Debe cambiar contraseña" },
        Filas = _usuarios.Select(u => new object?[] { u.Usuario, u.Dni, u.Email, u.Rol, u.Estado, u.CambioPendienteTexto }).ToList()
    };

    private SeccionPdf SeccionRoles() => new()
    {
        Titulo = "Usuarios por rol",
        Columnas = new[] { "Rol", "Descripción", "Activos", "Inactivos", "Total", "Deben cambiar contraseña" },
        Filas = _roles.Select(r => new object?[] { r.Rol, r.Descripcion, r.Activos, r.Inactivos, r.Total, r.CambioPendiente }).ToList()
    };

    /// <summary>Informe completo del período en PDF.</summary>
    private void ExportarPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_backups == null)
        {
            MessageBox.Show("Generá los reportes del período antes de exportar.", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var indicadores = new List<IndicadorPdf>
            {
                new("Última copia", UltimaCopiaText.Text, UltimaCopiaDetalleText.Text),
                new("Copias en el período", CopiasText.Text, CopiasDetalleText.Text),
                new("Backup automático", AutomaticoText.Text, AutomaticoDetalleText.Text),
                new("Usuarios activos", UsuariosText.Text, UsuariosDetalleText.Text)
            };

            string? ruta = ExportadorPdf.ExportarInforme("Reporte del sistema", Periodo, "Reporte_sistema",
                indicadores, new[] { SeccionBackups(), SeccionUsuarios(), SeccionRoles() });

            if (ruta != null)
                MessageBox.Show($"Informe exportado en:\n{ruta}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo exportar el informe.\n\n{ex.Message}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Exporta a CSV la pestaña que se está viendo.</summary>
    private void ExportarCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? ruta = ReportesTabs.SelectedIndex switch
            {
                0 => ExportadorCsv.Exportar<FilaBackupDto>("Copias_de_seguridad", _backups?.Copias ?? new(),
                    ("Fecha", r => r.FechaTexto),
                    ("Tipo", r => r.Tipo),
                    ("Generada por", r => r.GeneradaPor),
                    ("Archivo", r => r.Archivo),
                    ("Tamaño (MB)", r => (double)r.TamanoMb),
                    ("Archivo .bak", r => r.EstadoTexto)),

                1 => ExportadorCsv.Exportar<UsuarioReporteDto>("Usuarios", _usuarios,
                    ("Usuario", r => r.Usuario),
                    ("DNI", r => r.Dni),
                    ("Email", r => r.Email),
                    ("Rol", r => r.Rol),
                    ("Estado", r => r.Estado),
                    ("Debe cambiar contraseña", r => r.CambioPendienteTexto)),

                _ => ExportadorCsv.Exportar<UsuariosPorRolDto>("Usuarios_por_rol", _roles,
                    ("Rol", r => r.Rol),
                    ("Descripción", r => r.Descripcion),
                    ("Activos", r => r.Activos),
                    ("Inactivos", r => r.Inactivos),
                    ("Total", r => r.Total),
                    ("Deben cambiar contraseña", r => r.CambioPendiente))
            };

            if (ruta != null)
                MessageBox.Show($"Reporte exportado en:\n{ruta}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo exportar.\n\n{ex.Message}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
