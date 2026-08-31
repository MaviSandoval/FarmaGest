using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmaGest.Dominio;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Implementación TEMPORAL en memoria, sin datos cargados todavía.
/// Devuelve el estado real de un sistema recién instalado (todo en cero /
/// vacío) en vez de datos de ejemplo, para que la demo no muestre
/// información inventada. Cuando FarmaGest.Datos tenga los repositorios
/// reales, esta clase se reemplaza por una que consulte la base — la
/// interfaz IDashboardService no cambia, así que la UI no se toca.
/// </summary>
public class DashboardServiceEnMemoria : IDashboardService
{
    public Task<DashboardResumen> ObtenerResumenAsync()
    {
        var resumen = new DashboardResumen
        {
            VentasDelDia = 0,
            VariacionVentasDelDiaPorcentaje = 0,
            VentasDelMes = 0,
            VariacionVentasDelMesPorcentaje = 0,
            RecetasAtendidasHoy = 0,
            ProductosActivos = 0,
            ProductosStockBajo = 0,

            VentasUltimos7Dias = new List<PuntoVentaDiaria>
            {
                new() { Dia = "Lun", Total = 0 },
                new() { Dia = "Mar", Total = 0 },
                new() { Dia = "Mié", Total = 0 },
                new() { Dia = "Jue", Total = 0 },
                new() { Dia = "Vie", Total = 0 },
                new() { Dia = "Sáb", Total = 0 },
                new() { Dia = "Dom", Total = 0 },
            },

            ProductosConStockBajo = new List<Producto>(),
            UltimasVentas = new List<Venta>(),
            ProximosVencimientos = new List<Producto>(),
        };

        return Task.FromResult(resumen);
    }
}