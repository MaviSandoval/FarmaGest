using System.Text.RegularExpressions;

namespace FarmaGest.Negocio.Servicios;

public static class ValidacionTransferencia
{
    public const string MensajeDuplicado = "Este número de operación ya fue registrado en otra transferencia";

    public static string Normalizar(string? numero)
    {
        var valor = numero?.Trim();
        if (string.IsNullOrEmpty(valor))
            throw new InvalidOperationException("Ingrese el número de operación o comprobante.");
        if (valor.Length is < 6 or > 50)
            throw new InvalidOperationException("El número de operación debe tener entre 6 y 50 caracteres.");
        if (!Regex.IsMatch(valor, @"\A[\p{L}\p{Nd}-]+\z"))
            throw new InvalidOperationException("El número de operación solo admite letras, números y guiones, sin espacios internos.");
        return valor;
    }
}
