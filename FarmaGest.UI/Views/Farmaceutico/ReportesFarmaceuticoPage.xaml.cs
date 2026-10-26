using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// Reportes del Farmacéutico (tablas cruzadas, filtradas por período):
///  0. Resumen del período (tablero) . Venta + DetalleVenta + Facturacion + Producto + Categoria + Receta + ObraSocial
///  1. Ventas por obra social ......... Venta + DetalleVenta + Recetas + Afiliados + Planes + ObraSocial
///  2. Productos más vendidos ......... DetalleVenta + Venta + Producto + CategoriasProductos
///  3. Recetas por obra social ........ Recetas + Afiliados + Planes + ObraSocial + Venta
///  4. Ventas por semana / mes ........ Venta + DetalleVenta + Facturacion + Recetas
/// El Farmacéutico es el puesto más alto de la farmacia: ve los datos de TODA la farmacia,
/// no solo sus ventas. Solo cuentan las ventas cobradas; las anuladas no suman.
/// </summary>
public partial class ReportesFarmaceuticoPage : Page
{
    private readonly ReporteService _reporteService;
    private readonly SesionUsuarioService _sesion;

    private List<ReporteObraSocialDto> _obrasSociales = new();
    private List<ReporteProductoDto> _productos = new();
    private List<ReporteRecetasDto> _recetas = new();
    private List<ReportePeriodoDto> _periodos = new();
    private ResumenReportesDto? _resumen;

    public ReportesFarmaceuticoPage(ReporteService reporteService, SesionUsuarioService sesion)
    {
        InitializeComponent();
        _reporteService = reporteService;
        _sesion = sesion;

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
            _recetas = await _reporteService.RecetasPorObraSocialAsync(desde, hasta);
            _periodos = await _reporteService.VentasPorPeriodoAsync(desde, hasta, PorMes);

            ObraSocialGrid.ItemsSource = _obrasSociales;
            ProductosGrid.ItemsSource = _productos;
            RecetasGrid.ItemsSource = _recetas;
            PeriodoGrid.ItemsSource = _periodos;

            _resumen = await _reporteService.ResumenAsync(desde, hasta);
            Resumen.Mostrar(_resumen);
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
        Graficos.Dona(RecetasChart, new[]
            {
                ("Validadas", (double)_recetas.Sum(r => r.Validadas)),
                ("Pendientes", (double)_recetas.Sum(r => r.Pendientes)),
                ("Rechazadas", (double)_recetas.Sum(r => r.Rechazadas))
            },
            new[] { Graficos.Verde, Graficos.Ambar, Graficos.Rojo });
        Graficos.Mostrar(RecetasChartCard, _recetas.Count > 0);

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

    private async void ExportarPdf_Click(object sender, RoutedEventArgs e) => await ExportarInformeAsync();

    // =========================================================
    // INFORME PDF COMPLETO DEL PERÍODO
    // =========================================================
    private async Task ExportarInformeAsync()
    {
        if (DesdePicker.SelectedDate is not DateTime desde || HastaPicker.SelectedDate is not DateTime hasta || _resumen == null)
        {
            MessageBox.Show("Generá los reportes del período antes de exportar.", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var porObraSocial = await _reporteService.ProductosPorObraSocialAsync(desde, hasta);
            var r = _resumen;

            var indicadores = new List<IndicadorPdf>
            {
                new("Facturación", $"$ {r.Facturacion:N0}",
                    r.Variacion is decimal v ? $"{(v >= 0 ? "+" : "")}{v:N1} % respecto al período anterior" : "Sin ventas en el período anterior"),
                new("Ventas cobradas", r.Ventas.ToString("N0"), $"Ticket promedio $ {r.TicketPromedio:N0}"),
                new("Unidades vendidas", r.Unidades.ToString("N0"), $"{r.UnidadesConReceta:N0} con receta"),
                new("Ventas con obra social", $"{r.PorcentajeConObraSocial:N0} %", $"Cobertura descontada $ {r.CoberturaDescontada:N0}")
            };

            // En "por obra social" el nombre se muestra solo en la primera fila de cada grupo
            string? anterior = null;
            var filasPorObra = porObraSocial.Select(p =>
            {
                var fila = new object?[] { p.ObraSocial == anterior ? "" : p.ObraSocial, p.Puesto, p.Producto, p.Unidades, p.Cubierto, p.Total };
                anterior = p.ObraSocial;
                return fila;
            }).ToList();

            var secciones = new List<SeccionPdf>
            {
                new()
                {
                    Titulo = "Ventas por obra social",
                    Descripcion = "Total vendido, lo que cubrió la obra social y lo que pagó el cliente.",
                    Columnas = new[] { "Obra social", "Ventas", "Total vendido", "Cubierto por la OS", "Pagado por el cliente" },
                    Filas = _obrasSociales.Select(o => new object?[] { o.ObraSocial, o.Ventas, o.TotalVendido, o.TotalCubierto, o.PagadoCliente }).ToList()
                },
                new()
                {
                    Titulo = "Productos más vendidos por obra social",
                    Descripcion = "Los 5 productos con más unidades dentro de cada obra social (Particular = ventas sin receta).",
                    Columnas = new[] { "Obra social", "#", "Producto", "Unidades", "Cubierto por la OS", "Facturación" },
                    Filas = filasPorObra
                },
                new()
                {
                    Titulo = "Productos más vendidos",
                    Descripcion = "Ranking general por unidades vendidas.",
                    Columnas = new[] { "#", "Producto", "Categoría", "Unidades", "Facturación" },
                    Filas = _productos.Select(p => new object?[] { p.Puesto, p.Producto, p.Categoria, p.Unidades, p.Total }).ToList()
                },
                new()
                {
                    Titulo = "Ventas por categoría",
                    Columnas = new[] { "Categoría", "Unidades", "Facturación", "% del total" },
                    Filas = r.Categorias.Select(c => new object?[] { c.Nombre, c.Cantidad, c.Importe, $"{c.Porcentaje:N1} %" }).ToList()
                },
                new()
                {
                    Titulo = "Medios de pago",
                    Columnas = new[] { "Medio de pago", "Cobros", "Importe", "% del total" },
                    Filas = r.MediosPago.Select(m => new object?[] { m.Nombre, m.Cantidad, m.Importe, $"{m.Porcentaje:N1} %" }).ToList()
                },
                new()
                {
                    Titulo = "Recetas por obra social",
                    Descripcion = "Según la fecha de emisión de la receta.",
                    Columnas = new[] { "Obra social", "Recibidas", "Pendientes", "Validadas", "Rechazadas", "Usadas en ventas" },
                    Filas = _recetas.Select(x => new object?[] { x.ObraSocial, x.Total, x.Pendientes, x.Validadas, x.Rechazadas, x.Usadas }).ToList()
                },
                new()
                {
                    Titulo = PorMes ? "Ventas por mes" : "Ventas por semana",
                    Columnas = new[] { "Período", "Ventas", "Con receta", "Total vendido", "Cubierto por la OS", "Pagado por el cliente", "Ticket promedio" },
                    Filas = _periodos.Select(x => new object?[] { x.Periodo, x.Ventas, x.ConReceta, x.TotalVendido, x.TotalCubierto, x.PagadoCliente, x.TicketPromedio }).ToList()
                }
            };

            string? ruta = ExportadorPdf.ExportarInforme(
                "Informe de ventas y recetas",
                $"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · Toda la farmacia · Solo ventas cobradas",
                "Informe_ventas_recetas", indicadores, secciones);

            if (ruta != null)
                MessageBox.Show($"Informe exportado en:\n{ruta}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo exportar el informe.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

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

                2 => Exp<ReporteRecetasDto>("Recetas por obra social", "Recetas_por_obra_social", _recetas,
                    ("Obra social", r => r.ObraSocial),
                    ("Recibidas", r => r.Total),
                    ("Pendientes", r => r.Pendientes),
                    ("Validadas", r => r.Validadas),
                    ("Rechazadas", r => r.Rechazadas),
                    ("Usadas en ventas", r => r.Usadas)),

                _ => Exp<ReportePeriodoDto>(PorMes ? "Ventas por mes" : "Ventas por semana",
                    PorMes ? "Ventas_por_mes" : "Ventas_por_semana", _periodos,
                    ("Período", r => r.Periodo),
                    ("Ventas", r => r.Ventas),
                    ("Con receta", r => r.ConReceta),
                    ("Total vendido", r => r.TotalVendido),
                    ("Cubierto por la obra social", r => r.TotalCubierto),
                    ("Pagado por el cliente", r => r.PagadoCliente),
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
