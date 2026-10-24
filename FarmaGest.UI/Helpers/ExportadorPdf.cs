using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FarmaGest.UI.Helpers;

/// <summary>
/// Exporta un reporte a PDF con QuestPDF: encabezado con título y período,
/// tabla con los colores de FarmaGest y pie con fecha de generación y número de página.
/// Recibe las mismas columnas que ExportadorCsv, así cada reporte se define una sola vez.
/// </summary>
public static class ExportadorPdf
{
    private static readonly CultureInfo Cultura = new("es-AR");
    private const string ColorMarca = "#1F7A6C";

    static ExportadorPdf()
    {
        // Licencia gratuita de QuestPDF (uso académico / organizaciones chicas)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Pide dónde guardar y genera el PDF. Devuelve la ruta, o null si se canceló.</summary>
    public static string? Exportar<T>(string titulo, string subtitulo, string nombreSugerido, IEnumerable<T> filas,
        params (string Titulo, Func<T, object?> Valor)[] columnas)
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

        var lista = filas.ToList();

        Document.Create(documento => documento.Page(pagina =>
        {
            // Con muchas columnas, hoja apaisada
            pagina.Size(columnas.Length > 5 ? PageSizes.A4.Landscape() : PageSizes.A4);
            pagina.Margin(30);
            pagina.DefaultTextStyle(t => t.FontSize(10));

            pagina.Header().Column(encabezado =>
            {
                encabezado.Item().Text("FarmaGest").FontSize(9).SemiBold().FontColor(ColorMarca);
                encabezado.Item().Text(titulo).FontSize(16).Bold();
                encabezado.Item().Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken1);
                encabezado.Item().PaddingTop(6).LineHorizontal(1).LineColor(ColorMarca);
            });

            pagina.Content().PaddingTop(10).Table(tabla =>
            {
                tabla.ColumnsDefinition(definicion =>
                {
                    foreach (var _ in columnas)
                        definicion.RelativeColumn();
                });

                tabla.Header(cabecera =>
                {
                    foreach (var columna in columnas)
                        cabecera.Cell().Background(ColorMarca).Padding(4)
                            .Text(columna.Titulo).FontColor(Colors.White).SemiBold();
                });

                for (int i = 0; i < lista.Count; i++)
                {
                    var fondo = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    foreach (var columna in columnas)
                    {
                        object? valor = columna.Valor(lista[i]);
                        var celda = tabla.Cell().Background(fondo)
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        // Los números se alinean a la derecha
                        (EsNumero(valor) ? celda.AlignRight() : celda).Text(Formatear(valor));
                    }
                }

                if (lista.Count == 0)
                    tabla.Cell().ColumnSpan((uint)columnas.Length).Padding(8)
                        .Text("Sin datos para el período elegido.").Italic();
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

    private static bool EsNumero(object? valor) => valor is int or long or decimal or double;

    private static string Formatear(object? valor) => valor switch
    {
        null => "",
        DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm", Cultura),
        decimal importe => "$ " + importe.ToString("N2", Cultura),
        IFormattable formateable => formateable.ToString(null, Cultura),
        _ => valor.ToString() ?? ""
    };
}
