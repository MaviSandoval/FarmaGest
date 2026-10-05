using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>Reportes cruzados del período elegido. Las ventas anuladas no suman.</summary>
public partial class ReportesPage : Page
{
    private readonly ReporteService _reporteService;

    private List<ReporteObraSocialDto> _obrasSociales = new();
    private List<ReporteProductoDto> _productos = new();
    private List<ReporteCierreDto> _cierres = new();
    private List<ReporteFarmaceuticoDto> _farmaceuticos = new();

    public ReportesPage(ReporteService reporteService)
    {
        InitializeComponent();
        _reporteService = reporteService;

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
            _obrasSociales = await _reporteService.VentasPorObraSocialAsync(desde, hasta);
            _productos = await _reporteService.ProductosMasVendidosAsync(desde, hasta);
            _cierres = await _reporteService.CierresDeCajaAsync(desde, hasta);
            _farmaceuticos = await _reporteService.VentasPorFarmaceuticoAsync(desde, hasta);

            ObraSocialGrid.ItemsSource = _obrasSociales;
            ProductosGrid.ItemsSource = _productos;
            CierresGrid.ItemsSource = _cierres;
            FarmaceuticosGrid.ItemsSource = _farmaceuticos;
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
                0 => ExportadorCsv.Exportar<ReporteObraSocialDto>("Ventas_por_obra_social", _obrasSociales,
                    ("Obra social", r => r.ObraSocial),
                    ("Ventas", r => r.Ventas),
                    ("Total vendido", r => r.TotalVendido),
                    ("Cubierto por la obra social", r => r.TotalCubierto),
                    ("Pagado por el cliente", r => r.PagadoCliente)),

                1 => ExportadorCsv.Exportar<ReporteProductoDto>("Productos_mas_vendidos", _productos,
                    ("Puesto", r => r.Puesto),
                    ("Producto", r => r.Producto),
                    ("Categoría", r => r.Categoria),
                    ("Unidades", r => r.Unidades),
                    ("Total vendido", r => r.Total)),

                2 => ExportadorCsv.Exportar<ReporteCierreDto>("Cierres_de_caja", _cierres,
                    ("Fecha", r => r.Fecha),
                    ("Caja", r => r.Caja),
                    ("Cajero", r => r.Cajero),
                    ("Esperado", r => r.Esperado),
                    ("Contado", r => r.Contado),
                    ("Diferencia", r => r.Diferencia)),

                _ => ExportadorCsv.Exportar<ReporteFarmaceuticoDto>("Ventas_por_farmaceutico", _farmaceuticos,
                    ("Farmacéutico", r => r.Farmaceutico),
                    ("Ventas", r => r.Ventas),
                    ("Con receta", r => r.ConReceta),
                    ("Anuladas", r => r.Anuladas),
                    ("Total vendido", r => r.Total))
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
