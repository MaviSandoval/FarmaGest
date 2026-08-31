using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace FarmaGest.UI.Views.Administrador;

public partial class DashboardPage : Page
{
    private readonly IDashboardService _dashboardService;

    public DashboardPage(IDashboardService dashboardService)
    {
        InitializeComponent();
        _dashboardService = dashboardService;
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        var resumen = await _dashboardService.ObtenerResumenAsync();

        VentasDiaText.Text = $"$ {resumen.VentasDelDia:N0}";
        VariacionDiaText.Text = $"{(resumen.VariacionVentasDelDiaPorcentaje >= 0 ? "+" : "")}{resumen.VariacionVentasDelDiaPorcentaje:N0}% respecto a ayer";

        VentasMesText.Text = $"$ {resumen.VentasDelMes:N0}";
        VariacionMesText.Text = $"{(resumen.VariacionVentasDelMesPorcentaje >= 0 ? "+" : "")}{resumen.VariacionVentasDelMesPorcentaje:N0}% respecto al mes pasado";

        RecetasHoyText.Text = resumen.RecetasAtendidasHoy.ToString();
        ProductosActivosText.Text = resumen.ProductosActivos.ToString("N0");
        StockBajoCountText.Text = resumen.ProductosStockBajo.ToString();

        StockBajoList.ItemsSource = resumen.ProductosConStockBajo;
        UltimasVentasGrid.ItemsSource = resumen.UltimasVentas;
        VencimientosList.ItemsSource = resumen.ProximosVencimientos;

        ConfigurarGrafico(resumen.VentasUltimos7Dias);

        MostrarEstadoVacioSiCorresponde(resumen);
    }

    private void MostrarEstadoVacioSiCorresponde(Dominio.DashboardResumen resumen)
    {
        bool sinStockBajo = resumen.ProductosConStockBajo == null || resumen.ProductosConStockBajo.Count == 0;
        StockBajoVacioText.Visibility = sinStockBajo ? Visibility.Visible : Visibility.Collapsed;
        StockBajoList.Visibility = sinStockBajo ? Visibility.Collapsed : Visibility.Visible;
        VerTodoStockBajoLink.Visibility = sinStockBajo ? Visibility.Collapsed : Visibility.Visible;

        bool sinVentas = resumen.UltimasVentas == null || resumen.UltimasVentas.Count == 0;
        UltimasVentasVacioText.Visibility = sinVentas ? Visibility.Visible : Visibility.Collapsed;
        UltimasVentasGrid.Visibility = sinVentas ? Visibility.Collapsed : Visibility.Visible;
        VerTodasVentasLink.Visibility = sinVentas ? Visibility.Collapsed : Visibility.Visible;

        bool sinVencimientos = resumen.ProximosVencimientos == null || resumen.ProximosVencimientos.Count == 0;
        VencimientosVacioText.Visibility = sinVencimientos ? Visibility.Visible : Visibility.Collapsed;
        VencimientosList.Visibility = sinVencimientos ? Visibility.Collapsed : Visibility.Visible;
        VerTodosVencimientosLink.Visibility = sinVencimientos ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ConfigurarGrafico(List<Dominio.PuntoVentaDiaria> puntos)
    {
        VentasSemanaChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = puntos.Select(p => (double)p.Total).ToArray(),
                Stroke = new SolidColorPaint(new SKColor(0x1F, 0x7A, 0x6C), 3),
                Fill = new SolidColorPaint(new SKColor(0x1F, 0x7A, 0x6C, 40)),
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(new SKColor(0x1F, 0x7A, 0x6C), 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
            }
        };

        VentasSemanaChart.XAxes = new[] { new Axis { Labels = puntos.Select(p => p.Dia).ToArray() } };
        VentasSemanaChart.YAxes = new[] { new Axis { Labeler = v => $"${v / 1000:N0}k" } };
    }

    private void VerTodoStockBajo_Click(object sender, RoutedEventArgs e) { /* Navegar a Stock */ }
    private void VerTodasVentas_Click(object sender, RoutedEventArgs e) { /* Navegar a Ventas */ }
    private void VerTodosVencimientos_Click(object sender, RoutedEventArgs e) { /* Navegar a Stock (vencimientos) */ }
}