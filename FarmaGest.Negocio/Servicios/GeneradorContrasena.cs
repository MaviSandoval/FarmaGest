using System;
using System.Text;

namespace FarmaGest.Negocio.Servicios;

public static class GeneradorContrasena
{
    private const string Caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    /// <summary>Genera una contraseña temporal legible (sin caracteres ambiguos como 0/O, 1/l).</summary>
    public static string Generar(int longitud = 10)
    {
        var random = Random.Shared;
        var sb = new StringBuilder(longitud);
        for (int i = 0; i < longitud; i++)
            sb.Append(Caracteres[random.Next(Caracteres.Length)]);
        return sb.ToString();
    }
}