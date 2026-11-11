namespace FarmaGest.Negocio.Servicios;

internal static class CalculosCaja
{
    internal static bool EsEfectivo(string medio)
        => string.Equals(medio, "Efectivo", StringComparison.OrdinalIgnoreCase);

    internal static decimal EfectivoEsperado(decimal inicial, decimal efectivo)
        => inicial + efectivo;
}
