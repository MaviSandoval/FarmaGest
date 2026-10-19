using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// Reportes del Farmacéutico (tablas cruzadas, filtradas por período):
///  1. Mis ventas por obra social ..... Venta + DetalleVenta + Recetas + Afiliados + Planes + ObraSocial
///  2. Productos que más vendí ........ DetalleVenta + Venta + Producto + CategoriasProductos
///  3. Recetas por obra social ........ Recetas + Afiliados + Planes + ObraSocial + Venta
/// Los dos primeros solo cuentan las ventas del usuario logueado; las anuladas no suman.
/// </summary>
public partial class ReportesFarmaceuticoPage : Page
{
    private readonly ReporteService _reporteService;
    private readonly SesionUsuarioService _sesion;

    private List<ReporteObraSocialDto> _obrasSociales = new();
    private List<ReporteProductoDto> _productos = new();
    private List<ReporteRecetasDto> _recetas = new();

    public ReportesFarmaceuticoPage(ReporteService reporteService, SesionUsuarioService sesion)
    {
        InitializeComponent();
        _reporteService = reporteService;
        _sesion = sesion;

        DesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        HastaPicker.SelectedDate = DateTime.Today;

        Loaded += async (_, _) => await GenerarAsync();
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
            int? usuarioId = _sesion.UsuarioActual?.Id;

            _obrasSociales = await _reporteService.VentasPorObraSocialAsync(desde, hasta, usuarioId);
            _productos = await _reporteService.ProductosMasVendidosAsync(desde, hasta, usuarioId: usuarioId);
            _recetas = await _reporteService.RecetasPorObraSocialAsync(desde, hasta);

            ObraSocialGrid.ItemsSource = _obrasSociales;
            ProductosGrid.ItemsSource = _productos;
            RecetasGrid.ItemsSource = _recetas;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron generar los reportes.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Generar_Click(object sender, RoutedEventArgs e) => await GenerarAsync();

    /// <summary>Exporta a CSV el reporte de la pestaña que se está viendo.</summary>
    private void Exportar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? ruta = ReportesTabs.SelectedIndex switch
            {
                0 => ExportadorCsv.Exportar<ReporteObraSocialDto>("Mis_ventas_por_obra_social", _obrasSociales,
                    ("Obra social", r => r.ObraSocial),
                    ("Ventas", r => r.Ventas),
                    ("Total vendido", r => r.TotalVendido),
                    ("Cubierto por la obra social", r => r.TotalCubierto),
                    ("Pagado por el cliente", r => r.PagadoCliente)),

                1 => ExportadorCsv.Exportar<ReporteProductoDto>("Productos_que_mas_vendi", _productos,
                    ("Puesto", r => r.Puesto),
                    ("Producto", r => r.Producto),
                    ("Categoría", r => r.Categoria),
                    ("Unidades", r => r.Unidades),
                    ("Total vendido", r => r.Total)),

                _ => ExportadorCsv.Exportar<ReporteRecetasDto>("Recetas_por_obra_social", _recetas,
                    ("Obra social", r => r.ObraSocial),
                    ("Recibidas", r => r.Total),
                    ("Pendientes", r => r.Pendientes),
                    ("Validadas", r => r.Validadas),
                    ("Rechazadas", r => r.Rechazadas),
                    ("Usadas en ventas", r => r.Usadas))
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
