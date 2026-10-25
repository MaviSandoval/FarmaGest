using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private List<ReportePeriodoDto> _periodos = new();
    private List<ReporteMedioPagoDto> _mediosPago = new();

    public ReportesPage(ReporteService reporteService)
    {
        InitializeComponent();
        _reporteService = reporteService;

        DesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
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
            _obrasSociales = await _reporteService.VentasPorObraSocialAsync(desde, hasta);
            _productos = await _reporteService.ProductosMasVendidosAsync(desde, hasta);
            _cierres = await _reporteService.CierresDeCajaAsync(desde, hasta);
            _farmaceuticos = await _reporteService.VentasPorFarmaceuticoAsync(desde, hasta);
            _periodos = await _reporteService.VentasPorPeriodoAsync(desde, hasta, PorMes);
            _mediosPago = await _reporteService.CobrosPorMedioDePagoAsync(desde, hasta);

            ObraSocialGrid.ItemsSource = _obrasSociales;
            ProductosGrid.ItemsSource = _productos;
            CierresGrid.ItemsSource = _cierres;
            FarmaceuticosGrid.ItemsSource = _farmaceuticos;
            PeriodoGrid.ItemsSource = _periodos;
            MediosPagoGrid.ItemsSource = _mediosPago;

            Resumen.Mostrar(await _reporteService.ResumenAsync(desde, hasta));
            ActualizarGraficos();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron generar los reportes.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // =========================================================
    // GRÁFICOS DE LAS PESTAÑAS (los que no están en el resumen)
    // =========================================================
    private void ActualizarGraficos()
    {
        // Solo cajas cerradas (las abiertas todavía no tienen efectivo contado)
        var cerradas = _cierres.Where(c => !c.Abierta).OrderBy(c => c.Apertura).TakeLast(12).ToList();
        Graficos.BarrasAgrupadas(CajasChart, cerradas.Select(c => $"Caja {c.Caja}"), Graficos.Moneda,
            ("Efectivo esperado", cerradas.Select(c => (double)c.Esperado), Graficos.Gris),
            ("Efectivo contado", cerradas.Select(c => (double)(c.Contado ?? 0)), Graficos.Verde));
        Graficos.Mostrar(CajasChartCard, cerradas.Count > 0);

        Graficos.Barras(FarmaceuticosChart, _farmaceuticos.Select(f => f.Farmaceutico), "Total vendido",
            _farmaceuticos.Select(f => (double)f.Total), Graficos.VerdeClaro, Graficos.Moneda);
        Graficos.Mostrar(FarmaceuticosChartCard, _farmaceuticos.Count > 0);

        Graficos.BarrasYLinea(PeriodoChart, _periodos.Select(p => p.Etiqueta),
            "Total vendido", _periodos.Select(p => (double)p.TotalVendido),
            "Cantidad de ventas", _periodos.Select(p => (double)p.Ventas));
        Graficos.Mostrar(PeriodoChartCard, _periodos.Count > 0);
    }

    // =========================================================
    // ACCESOS RÁPIDOS DE PERÍODO
    // =========================================================
    private async void Rapido_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int dias))
        {
            HastaPicker.SelectedDate = DateTime.Today;
            DesdePicker.SelectedDate = DateTime.Today.AddDays(-(dias - 1));
            await GenerarAsync();
        }
    }

    // Las tablas y los gráficos se quedan con la rueda del mouse:
    // se reenvía al scroll de la página para que se pueda bajar desde cualquier lado.
    private void ReenviarRueda(object sender, MouseWheelEventArgs e)
    {
        ScrollPrincipal.ScrollToVerticalOffset(ScrollPrincipal.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private async void Generar_Click(object sender, RoutedEventArgs e) => await GenerarAsync();

    private bool PorMes => AgrupacionCombo.SelectedIndex == 1;

    private async void Agrupacion_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            await GenerarAsync();
    }

    private void ExportarCsv_Click(object sender, RoutedEventArgs e) => Exportar(pdf: false);

    private void ExportarPdf_Click(object sender, RoutedEventArgs e) => Exportar(pdf: true);

    /// <summary>Exporta a CSV o PDF el reporte de la pestaña que se está viendo.</summary>
    private void Exportar(bool pdf)
    {
        string periodo = $"Período: {DesdePicker.SelectedDate:dd/MM/yyyy} al {HastaPicker.SelectedDate:dd/MM/yyyy}";

        // Mismas columnas para los dos formatos
        string? Exp<T>(string titulo, string archivo, IEnumerable<T> filas,
            params (string Titulo, Func<T, object?> Valor)[] columnas) =>
            pdf ? ExportadorPdf.Exportar(titulo, periodo, archivo, filas, columnas)
                : ExportadorCsv.Exportar(archivo, filas, columnas);

        try
        {
            string? ruta = ReportesTabs.SelectedIndex switch
            {
                0 => Exp<ReporteObraSocialDto>("Ventas por obra social", "Ventas_por_obra_social", _obrasSociales,
                    ("Obra social", r => r.ObraSocial),
                    ("Ventas", r => r.Ventas),
                    ("Total vendido", r => r.TotalVendido),
                    ("Cubierto por la obra social", r => r.TotalCubierto),
                    ("Pagado por el cliente", r => r.PagadoCliente)),

                1 => Exp<ReporteProductoDto>("Productos más vendidos", "Productos_mas_vendidos", _productos,
                    ("Puesto", r => r.Puesto),
                    ("Producto", r => r.Producto),
                    ("Categoría", r => r.Categoria),
                    ("Unidades", r => r.Unidades),
                    ("Total vendido", r => r.Total)),

                2 => Exp<ReporteCierreDto>("Cajas", "Cajas", _cierres,
                    ("Caja", r => r.Caja),
                    ("Cajero", r => r.Cajero),
                    ("Apertura", r => r.AperturaTexto),
                    ("Cierre", r => r.CierreTexto),
                    ("Monto inicial", r => r.MontoInicial),
                    ("Efectivo", r => r.Efectivo),
                    ("Otros medios", r => r.OtrosMedios),
                    ("Efectivo esperado", r => r.Esperado),
                    ("Efectivo contado", r => r.Contado),
                    ("Diferencia", r => r.Diferencia),
                    ("Resultado", r => r.Estado)),

                3 => Exp<ReporteFarmaceuticoDto>("Ventas por farmacéutico", "Ventas_por_farmaceutico", _farmaceuticos,
                    ("Farmacéutico", r => r.Farmaceutico),
                    ("Ventas", r => r.Ventas),
                    ("Con receta", r => r.ConReceta),
                    ("Anuladas", r => r.Anuladas),
                    ("Total vendido", r => r.Total)),

                4 => Exp<ReportePeriodoDto>(PorMes ? "Ventas por mes" : "Ventas por semana",
                    PorMes ? "Ventas_por_mes" : "Ventas_por_semana", _periodos,
                    ("Período", r => r.Periodo),
                    ("Ventas", r => r.Ventas),
                    ("Con receta", r => r.ConReceta),
                    ("Total vendido", r => r.TotalVendido),
                    ("Cubierto por la obra social", r => r.TotalCubierto),
                    ("Pagado por el cliente", r => r.PagadoCliente),
                    ("Ticket promedio", r => r.TicketPromedio)),

                _ => Exp<ReporteMedioPagoDto>("Cobros por medio de pago", "Cobros_por_medio_de_pago", _mediosPago,
                    ("Medio de pago", r => r.Medio),
                    ("Cobros", r => r.Cobros),
                    ("Cajas", r => r.Cajas),
                    ("Total cobrado", r => r.Importe),
                    ("% del total", r => $"{r.Porcentaje:N1} %"),
                    ("Ticket promedio", r => r.TicketPromedio))
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
