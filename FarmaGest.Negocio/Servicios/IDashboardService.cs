using FarmaGest.Dominio;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Contrato para obtener los datos agregados de la pantalla de Inicio.
/// La UI depende solo de esta interfaz, nunca de la implementación.
/// </summary>
public interface IDashboardService
{
    Task<DashboardResumen> ObtenerResumenAsync();
}