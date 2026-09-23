using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaGest.UI.Helpers;

/// <summary>Tipos de dato que puede aceptar un campo de texto.</summary>
public enum TipoDato
{
    Libre,          // sin restricción de caracteres
    Entero,         // solo dígitos: DNI, stock, cantidades
    Decimal,        // importes: dígitos y un separador (, o .) con hasta 2 decimales
    SoloLetras,     // nombres y apellidos: letras (con tildes y ñ), espacios, punto, apóstrofe y guion
    Alfanumerico,   // búsquedas, matrículas, N° de afiliado: letras, dígitos, espacios, punto, guion y barra
    Email           // sin espacios (el formato completo se valida al guardar)
}

/// <summary>
/// Validación de tipos de dato en los TextBox.
/// Bloquea los caracteres inválidos al tipear y al pegar.
///
/// Uso en XAML:
///   xmlns:h="clr-namespace:FarmaGest.UI.Helpers"
///   &lt;TextBox h:ValidacionEntrada.Tipo="Entero" MaxLength="8" /&gt;
/// </summary>
public static class ValidacionEntrada
{
    public static readonly DependencyProperty TipoProperty =
        DependencyProperty.RegisterAttached(
            "Tipo",
            typeof(TipoDato),
            typeof(ValidacionEntrada),
            new PropertyMetadata(TipoDato.Libre, AlCambiarTipo));

    public static TipoDato GetTipo(DependencyObject elemento) =>
        (TipoDato)elemento.GetValue(TipoProperty);

    public static void SetTipo(DependencyObject elemento, TipoDato valor) =>
        elemento.SetValue(TipoProperty, valor);

    // =========================================================
    // REGLAS POR TIPO
    // =========================================================
    private static readonly Regex ReglaEntero       = new(@"^\d*$");
    private static readonly Regex ReglaDecimal      = new(@"^\d*([.,]\d{0,2})?$");
    private static readonly Regex ReglaSoloLetras   = new(@"^[\p{L} .'\-]*$");
    private static readonly Regex ReglaAlfanumerico = new(@"^[\p{L}\d .\-/]*$");
    private static readonly Regex ReglaEmail        = new(@"^\S*$");
    private static readonly Regex FormatoEmail      = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    /// <summary>Indica si un texto completo cumple con el tipo de dato.</summary>
    public static bool EsValido(TipoDato tipo, string texto) => tipo switch
    {
        TipoDato.Entero       => ReglaEntero.IsMatch(texto),
        TipoDato.Decimal      => ReglaDecimal.IsMatch(texto),
        TipoDato.SoloLetras   => ReglaSoloLetras.IsMatch(texto),
        TipoDato.Alfanumerico => ReglaAlfanumerico.IsMatch(texto),
        TipoDato.Email        => ReglaEmail.IsMatch(texto),
        _                     => true
    };

    /// <summary>Valida el formato completo de un email (usuario@dominio.ext).</summary>
    public static bool EsEmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email) && FormatoEmail.IsMatch(email.Trim());

    /// <summary>
    /// Convierte un importe escrito con coma o punto ("1250,50" o "1250.50") a decimal.
    /// Devuelve null si el texto no es un número válido.
    /// </summary>
    public static decimal? LeerDecimal(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var normalizado = texto.Trim().Replace(',', '.');

        return decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint,
                                CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;
    }

    /// <summary>Convierte un texto a entero. Devuelve null si no es válido.</summary>
    public static int? LeerEntero(string? texto) =>
        int.TryParse(texto?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;

    // =========================================================
    // ENGANCHE CON EL TEXTBOX
    // =========================================================
    private static void AlCambiarTipo(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox cajaTexto)
            return;

        cajaTexto.PreviewTextInput -= AlEscribir;
        cajaTexto.PreviewKeyDown -= AlPresionarTecla;
        DataObject.RemovePastingHandler(cajaTexto, AlPegar);

        if ((TipoDato)e.NewValue == TipoDato.Libre)
            return;

        cajaTexto.PreviewTextInput += AlEscribir;
        cajaTexto.PreviewKeyDown += AlPresionarTecla;
        DataObject.AddPastingHandler(cajaTexto, AlPegar);
    }

    private static void AlEscribir(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox cajaTexto)
            e.Handled = !EsValido(GetTipo(cajaTexto), TextoResultante(cajaTexto, e.Text));
    }

    // La barra espaciadora no dispara PreviewTextInput en todos los casos: se controla acá
    private static void AlPresionarTecla(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && sender is TextBox cajaTexto)
            e.Handled = !EsValido(GetTipo(cajaTexto), TextoResultante(cajaTexto, " "));
    }

    private static void AlPegar(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox cajaTexto)
            return;

        var pegado = e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            ? e.DataObject.GetData(DataFormats.UnicodeText) as string
            : null;

        if (pegado == null || !EsValido(GetTipo(cajaTexto), TextoResultante(cajaTexto, pegado)))
            e.CancelCommand();
    }

    /// <summary>Cómo quedaría el texto si se acepta lo que el usuario escribió o pegó.</summary>
    private static string TextoResultante(TextBox cajaTexto, string nuevo)
    {
        var texto = cajaTexto.Text ?? string.Empty;
        var inicio = cajaTexto.SelectionStart;
        var largo = cajaTexto.SelectionLength;

        return texto.Remove(inicio, largo).Insert(inicio, nuevo);
    }
}
