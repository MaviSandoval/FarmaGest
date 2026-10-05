using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace FarmaGest.UI.Helpers;

/// <summary>
/// Exporta un listado a CSV (se abre directo con Excel).
/// Usa ";" como separador y UTF-8 con BOM para que Excel en español muestre bien tildes y decimales.
/// </summary>
public static class ExportadorCsv
{
    private static readonly CultureInfo Cultura = new("es-AR");

    /// <summary>Pide dónde guardar y escribe el archivo. Devuelve la ruta, o null si se canceló.</summary>
    public static string? Exportar<T>(string nombreSugerido, IEnumerable<T> filas,
        params (string Titulo, Func<T, object?> Valor)[] columnas)
    {
        var dialogo = new SaveFileDialog
        {
            Title = "Exportar a Excel (CSV)",
            FileName = $"{nombreSugerido}_{DateTime.Now:yyyy-MM-dd}.csv",
            Filter = "Archivo CSV (*.csv)|*.csv",
            DefaultExt = ".csv"
        };

        if (dialogo.ShowDialog() != true)
            return null;

        var texto = new StringBuilder();
        texto.AppendLine(string.Join(";", columnas.Select(c => Escapar(c.Titulo))));

        foreach (var fila in filas)
            texto.AppendLine(string.Join(";", columnas.Select(c => Escapar(Formatear(c.Valor(fila))))));

        File.WriteAllText(dialogo.FileName, texto.ToString(), new UTF8Encoding(true));
        return dialogo.FileName;
    }

    private static string Formatear(object? valor) => valor switch
    {
        null => "",
        DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm", Cultura),
        decimal numero => numero.ToString("0.00", Cultura),
        IFormattable formateable => formateable.ToString(null, Cultura),
        _ => valor.ToString() ?? ""
    };

    private static string Escapar(string texto) =>
        texto.Contains(';') || texto.Contains('"') || texto.Contains('\n')
            ? "\"" + texto.Replace("\"", "\"\"") + "\""
            : texto;
}
