using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;

namespace FarmaGest.UI.Helpers;

/// <summary>
/// Gráficos de los reportes (LiveCharts) con la paleta de FarmaGest.
/// Cada método recibe el control y los datos ya calculados; así las páginas
/// solo dicen QUÉ mostrar y el estilo queda igual en todos los reportes.
/// </summary>
public static class Graficos
{
    // ---------- Paleta (misma que App.xaml) ----------
    public static readonly SKColor Verde = SKColor.Parse("#1F7A6C");
    public static readonly SKColor VerdeClaro = SKColor.Parse("#2FA98C");
    public static readonly SKColor Celeste = SKColor.Parse("#22B8CF");
    public static readonly SKColor Ambar = SKColor.Parse("#E0A93B");
    public static readonly SKColor Rojo = SKColor.Parse("#D64550");
    public static readonly SKColor Gris = SKColor.Parse("#B7C6C3");

    private static readonly SKColor Texto = SKColor.Parse("#6B7B79");
    private static readonly SKColor Separador = SKColor.Parse("#E6EEEC");

    private static readonly SKColor[] Paleta =
        { Verde, Celeste, Ambar, VerdeClaro, Rojo, SKColor.Parse("#7C6FD6"), Gris };

    // ---------- Formatos ----------
    public static string Moneda(double v) =>
        Math.Abs(v) >= 1_000_000 ? $"$ {v / 1_000_000:N1} M" :
        Math.Abs(v) >= 10_000 ? $"$ {v / 1000:N0} k" :
        $"$ {v:N0}";

    public static string Entero(double v) => v.ToString("N0");

    /// <summary>Oculta la tarjeta del gráfico cuando el reporte no tiene datos.</summary>
    public static void Mostrar(FrameworkElement tarjeta, bool hayDatos) =>
        tarjeta.Visibility = hayDatos ? Visibility.Visible : Visibility.Collapsed;

    // =========================================================
    // BARRAS VERTICALES (una serie)
    // =========================================================
    public static void Barras(CartesianChart chart, IEnumerable<string> etiquetas, string nombre,
        IEnumerable<double> valores, SKColor color, Func<double, string> formato)
    {
        chart.Series = new ISeries[] { Columna(nombre, valores, color, formato) };
        EjesCategoria(chart, etiquetas, formato, leyenda: false);
    }

    // =========================================================
    // BARRAS AGRUPADAS (varias series lado a lado)
    // =========================================================
    public static void BarrasAgrupadas(CartesianChart chart, IEnumerable<string> etiquetas, Func<double, string> formato,
        params (string Nombre, IEnumerable<double> Valores, SKColor Color)[] series)
    {
        chart.Series = series.Select(s => (ISeries)Columna(s.Nombre, s.Valores, s.Color, null)).ToArray();
        EjesCategoria(chart, etiquetas, formato, leyenda: true);
    }

    // =========================================================
    // BARRAS APILADAS (las series suman en la misma barra)
    // =========================================================
    public static void BarrasApiladas(CartesianChart chart, IEnumerable<string> etiquetas, Func<double, string> formato,
        params (string Nombre, IEnumerable<double> Valores, SKColor Color)[] series)
    {
        chart.Series = series.Select(s => (ISeries)new StackedColumnSeries<double>
        {
            Name = s.Nombre,
            Values = s.Valores.ToArray(),
            Fill = new SolidColorPaint(s.Color),
            Stroke = null,
            MaxBarWidth = 46
        }).ToArray();

        EjesCategoria(chart, etiquetas, formato, leyenda: true);
    }

    // =========================================================
    // BARRAS HORIZONTALES (ranking: el primero queda arriba)
    // =========================================================
    public static void BarrasHorizontales(CartesianChart chart, IEnumerable<string> etiquetas, string nombre,
        IEnumerable<double> valores, SKColor color, Func<double, string> formato)
    {
        // RowSeries dibuja de abajo hacia arriba: se invierte para que el #1 quede arriba
        var nombres = etiquetas.Select(e => Recortar(e, 26)).Reverse().ToArray();
        var datos = valores.Reverse().ToArray();

        chart.Series = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = nombre,
                Values = datos,
                Fill = new SolidColorPaint(color),
                Stroke = null,
                MaxBarWidth = 22,
                Rx = 4,
                Ry = 4,
                DataLabelsPaint = new SolidColorPaint(Texto),
                DataLabelsSize = 11,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => formato(p.Model)
            }
        };

        chart.YAxes = new[]
        {
            new Axis { Labels = nombres, TextSize = 11, LabelsPaint = new SolidColorPaint(Texto), ShowSeparatorLines = false }
        };
        chart.XAxes = new[] { EjeValores(formato) };
        chart.LegendPosition = LegendPosition.Hidden;
    }

    // =========================================================
    // BARRAS + LÍNEA (importe en barras, cantidad en línea con su propio eje)
    // =========================================================
    public static void BarrasYLinea(CartesianChart chart, IEnumerable<string> etiquetas,
        string nombreBarras, IEnumerable<double> barras, string nombreLinea, IEnumerable<double> linea)
    {
        chart.Series = new ISeries[]
        {
            Columna(nombreBarras, barras, Verde, null),
            new LineSeries<double>
            {
                Name = nombreLinea,
                Values = linea.ToArray(),
                ScalesYAt = 1,
                Fill = null,
                Stroke = new SolidColorPaint(Ambar, 3),
                GeometrySize = 9,
                GeometryStroke = new SolidColorPaint(Ambar, 3),
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0.4
            }
        };

        EjesCategoria(chart, etiquetas, Moneda, leyenda: true);

        var ejeCantidad = EjeValores(Entero);
        ejeCantidad.Position = AxisPosition.End;
        ejeCantidad.ShowSeparatorLines = false;
        chart.YAxes = new[] { EjeValores(Moneda), ejeCantidad };
    }

    // =========================================================
    // EVOLUCIÓN (barras suaves; la última, la más reciente, resaltada)
    // =========================================================
    public static void EvolucionDestacada(CartesianChart chart, IEnumerable<string> etiquetas, IEnumerable<double> valores)
    {
        var datos = valores.ToArray();
        int ultimo = datos.Length - 1;

        // Dos series apiladas: una con todas menos la última y otra solo con la última.
        // Así la barra más reciente sale en verde oscuro sin depender de colores por punto.
        var anteriores = datos.Select((v, i) => i == ultimo ? 0 : v).ToArray();
        var actual = datos.Select((v, i) => i == ultimo ? v : 0).ToArray();

        chart.Series = new ISeries[]
        {
            BarraEvolucion("Anteriores", anteriores, SKColor.Parse("#BFE3D8")),
            BarraEvolucion("Actual", actual, Verde)
        };

        EjesCategoria(chart, etiquetas, Moneda, leyenda: false);
    }

    private static StackedColumnSeries<double> BarraEvolucion(string nombre, double[] valores, SKColor color) => new()
    {
        Name = nombre,
        Values = valores,
        Fill = new SolidColorPaint(color),
        Stroke = null,
        MaxBarWidth = 48,
        Rx = 6,
        Ry = 6,
        DataLabelsPaint = new SolidColorPaint(Texto),
        DataLabelsSize = 11,
        DataLabelsPosition = DataLabelsPosition.Top,
        DataLabelsFormatter = p => p.Model == 0 ? "" : Moneda(p.Model)
    };

    // =========================================================
    // DONA (porcentajes)
    // =========================================================
    public static void Dona(PieChart chart, IEnumerable<(string Nombre, double Valor)> porciones, SKColor[]? colores = null)
    {
        var lista = porciones.ToList();
        double total = lista.Sum(p => p.Valor);
        var paleta = colores ?? Paleta;

        chart.Series = lista
            .Select((p, i) => (Porcion: p, Color: paleta[i % paleta.Length]))
            .Where(x => x.Porcion.Valor > 0)
            .Select(x => (ISeries)new PieSeries<double>
            {
                Name = x.Porcion.Nombre,
                Values = new[] { x.Porcion.Valor },
                Fill = new SolidColorPaint(x.Color),
                Stroke = new SolidColorPaint(SKColors.White, 2),
                InnerRadius = 60,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsFormatter = _ => total == 0 ? "" : $"{x.Porcion.Valor / total:P0}"
            })
            .ToArray();

        chart.LegendPosition = LegendPosition.Right;
        chart.LegendTextPaint = new SolidColorPaint(Texto);
    }

    // =========================================================
    // PIEZAS COMUNES
    // =========================================================
    private static ColumnSeries<double> Columna(string nombre, IEnumerable<double> valores, SKColor color,
        Func<double, string>? etiqueta) => new()
    {
        Name = nombre,
        Values = valores.ToArray(),
        Fill = new SolidColorPaint(color),
        Stroke = null,
        MaxBarWidth = 42,
        Rx = 6,
        Ry = 6,
        DataLabelsPaint = etiqueta == null ? null : new SolidColorPaint(Texto),
        DataLabelsSize = 11,
        DataLabelsPosition = DataLabelsPosition.Top,
        DataLabelsFormatter = p => etiqueta == null ? "" : etiqueta(p.Model)
    };

    private static void EjesCategoria(CartesianChart chart, IEnumerable<string> etiquetas,
        Func<double, string> formato, bool leyenda)
    {
        var nombres = etiquetas.Select(e => Recortar(e, 18)).ToArray();

        chart.XAxes = new[]
        {
            new Axis
            {
                Labels = nombres,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(Texto),
                LabelsRotation = nombres.Length > 6 ? -20 : 0,
                ShowSeparatorLines = false
            }
        };
        chart.YAxes = new[] { EjeValores(formato) };
        chart.LegendPosition = leyenda ? LegendPosition.Bottom : LegendPosition.Hidden;
        chart.LegendTextPaint = new SolidColorPaint(Texto);
    }

    private static Axis EjeValores(Func<double, string> formato) => new()
    {
        Labeler = formato,
        MinLimit = 0,
        TextSize = 11,
        LabelsPaint = new SolidColorPaint(Texto),
        SeparatorsPaint = new SolidColorPaint(Separador, 1)
    };

    private static string Recortar(string texto, int max) =>
        texto.Length <= max ? texto : texto[..(max - 1)] + "…";
}
