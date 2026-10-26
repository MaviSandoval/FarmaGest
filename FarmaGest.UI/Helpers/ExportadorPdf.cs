using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FarmaGest.UI.Helpers;

/// <summary>Una tabla del PDF: título, descripción opcional, columnas y filas.</summary>
public class SeccionPdf
{
    public string Titulo { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string[] Columnas { get; init; } = Array.Empty<string>();
    public List<object?[]> Filas { get; init; } = new();

    /// <summary>Una columna es numérica si todos sus valores con dato son números (se alinea a la derecha).</summary>
    public bool EsNumerica(int columna)
    {
        var valores = Filas.Select(f => f[columna]).Where(v => v is not null && v is not "").ToList();
        return valores.Count > 0 && valores.All(ExportadorPdf.EsNumero);
    }
}

/// <summary>Un recuadro de indicador en el encabezado del informe.</summary>
public record IndicadorPdf(string Titulo, string Valor, string? Detalle = null);

/// <summary>
/// Exporta reportes a PDF con QuestPDF: encabezado con título y período, tablas con los
/// colores de FarmaGest y pie con fecha de generación y número de página.
///  - Exportar: una tabla (la pestaña que se está viendo).
///  - ExportarInforme: informe completo con indicadores y varias secciones.
/// </summary>
public static class ExportadorPdf
{
    private static readonly CultureInfo Cultura = new("es-AR");
    private const string ColorMarca = "#1F7A6C";
    private const string ColorSuave = "#E3F4EE";

    static ExportadorPdf()
    {
        // Licencia gratuita de QuestPDF (uso académico / organizaciones chicas)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Pide dónde guardar y genera el PDF de una tabla. Devuelve la ruta, o null si se canceló.</summary>
    public static string? Exportar<T>(string titulo, string subtitulo, string nombreSugerido, IEnumerable<T> filas,
        params (string Titulo, Func<T, object?> Valor)[] columnas)
    {
        var seccion = new SeccionPdf
        {
            Titulo = titulo,
            Columnas = columnas.Select(c => c.Titulo).ToArray(),
            Filas = filas.Select(f => columnas.Select(c => c.Valor(f)).ToArray()).ToList()
        };

        return Generar(titulo, subtitulo, nombreSugerido, Array.Empty<IndicadorPdf>(), new[] { seccion });
    }

    /// <summary>Pide dónde guardar y genera un informe con indicadores y varias tablas.</summary>
    public static string? ExportarInforme(string titulo, string subtitulo, string nombreSugerido,
        IReadOnlyList<IndicadorPdf> indicadores, IReadOnlyList<SeccionPdf> secciones) =>
        Generar(titulo, subtitulo, nombreSugerido, indicadores, secciones);

    // =========================================================
    // ARMADO DEL DOCUMENTO
    // =========================================================
    private static string? Generar(string titulo, string subtitulo, string nombreSugerido,
        IReadOnlyList<IndicadorPdf> indicadores, IReadOnlyList<SeccionPdf> secciones)
    {
        var dialogo = new SaveFileDialog
        {
            Title = "Exportar a PDF",
            FileName = $"{nombreSugerido}_{DateTime.Now:yyyy-MM-dd}.pdf",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf"
        };

        if (dialogo.ShowDialog() != true)
            return null;

        // Con muchas columnas, hoja apaisada
        bool apaisada = secciones.Any(s => s.Columnas.Length > 5);
        bool unaSola = secciones.Count == 1 && indicadores.Count == 0;

        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(apaisada ? PageSizes.A4.Landscape() : PageSizes.A4);
            pagina.Margin(30);
            pagina.DefaultTextStyle(t => t.FontSize(9.5f));

            pagina.Header().Column(encabezado =>
            {
                encabezado.Item().Text("FarmaGest").FontSize(9).SemiBold().FontColor(ColorMarca);
                encabezado.Item().Text(titulo).FontSize(16).Bold();
                encabezado.Item().Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken1);
                encabezado.Item().PaddingTop(6).LineHorizontal(1).LineColor(ColorMarca);
            });

            pagina.Content().PaddingTop(10).Column(contenido =>
            {
                contenido.Spacing(16);

                if (indicadores.Count > 0)
                    contenido.Item().Element(c => Indicadores(c, indicadores));

                foreach (var seccion in secciones)
                    contenido.Item().Element(c => Seccion(c, seccion, mostrarTitulo: !unaSola));
            });

            pagina.Footer().Row(pie =>
            {
                pie.RelativeItem().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                pie.RelativeItem().AlignRight().Text(texto =>
                {
                    texto.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        })).GeneratePdf(dialogo.FileName);

        return dialogo.FileName;
    }

    /// <summary>Recuadros de indicadores, de a 4 por fila.</summary>
    private static void Indicadores(IContainer contenedor, IReadOnlyList<IndicadorPdf> indicadores)
    {
        contenedor.Column(filas =>
        {
            filas.Spacing(8);

            foreach (var grupo in indicadores.Chunk(4))
            {
                filas.Item().Row(fila =>
                {
                    fila.Spacing(8);

                    foreach (var indicador in grupo)
                    {
                        fila.RelativeItem().Background(ColorSuave).Padding(8).Column(caja =>
                        {
                            caja.Item().Text(indicador.Titulo).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            caja.Item().Text(indicador.Valor).FontSize(14).Bold().FontColor(ColorMarca);
                            if (!string.IsNullOrEmpty(indicador.Detalle))
                                caja.Item().Text(indicador.Detalle).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    }

                    // Completa la fila para que los recuadros mantengan el mismo ancho
                    for (int i = grupo.Length; i < 4; i++)
                        fila.RelativeItem();
                });
            }
        });
    }

    /// <summary>Título de sección + tabla. Los encabezados se alinean igual que sus valores.</summary>
    private static void Seccion(IContainer contenedor, SeccionPdf seccion, bool mostrarTitulo)
    {
        var numericas = Enumerable.Range(0, seccion.Columnas.Length).Select(seccion.EsNumerica).ToArray();

        contenedor.Column(columna =>
        {
            if (mostrarTitulo)
            {
                columna.Item().Text(seccion.Titulo).FontSize(12.5f).Bold().FontColor(ColorMarca);
                if (!string.IsNullOrEmpty(seccion.Descripcion))
                    columna.Item().Text(seccion.Descripcion).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                columna.Item().Height(6);
            }

            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(definicion =>
                {
                    // Las columnas de texto son más anchas que las numéricas
                    foreach (bool numerica in numericas)
                        definicion.RelativeColumn(numerica ? 1f : 1.7f);
                });

                tabla.Header(cabecera =>
                {
                    for (int c = 0; c < seccion.Columnas.Length; c++)
                    {
                        var celda = cabecera.Cell().Background(ColorMarca).PaddingVertical(5).PaddingHorizontal(6);
                        (numericas[c] ? celda.AlignRight() : celda.AlignLeft())
                            .Text(seccion.Columnas[c]).FontColor(Colors.White).SemiBold();
                    }
                });

                for (int i = 0; i < seccion.Filas.Count; i++)
                {
                    var fondo = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    for (int c = 0; c < seccion.Columnas.Length; c++)
                    {
                        var celda = tabla.Cell().Background(fondo)
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(4).PaddingHorizontal(6);

                        (numericas[c] ? celda.AlignRight() : celda.AlignLeft()).Text(Formatear(seccion.Filas[i][c]));
                    }
                }

                if (seccion.Filas.Count == 0)
                    tabla.Cell().ColumnSpan((uint)seccion.Columnas.Length).Padding(8)
                        .Text("Sin datos para el período elegido.").Italic().FontColor(Colors.Grey.Darken1);
            });
        });
    }

    public static bool EsNumero(object? valor) => valor is int or long or decimal or double;

    private static string Formatear(object? valor) => valor switch
    {
        null => "",
        DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm", Cultura),
        decimal importe => "$ " + importe.ToString("N2", Cultura),
        IFormattable formateable => formateable.ToString(null, Cultura),
        _ => valor.ToString() ?? ""
    };
}
