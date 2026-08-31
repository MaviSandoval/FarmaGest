namespace FarmaGest.Dominio;

/// <summary>
/// Datos agregados que necesita la pantalla de Inicio. No es una entidad
/// persistida: la arma el servicio de Negocio combinando otras entidades.
/// </summary>
public class DashboardResumen
{
    public decimal VentasDelDia { get; set; }
    public decimal VariacionVentasDelDiaPorcentaje { get; set; }
    public decimal VentasDelMes { get; set; }
    public decimal VariacionVentasDelMesPorcentaje { get; set; }
    public int RecetasAtendidasHoy { get; set; }
    public int ProductosActivos { get; set; }
    public int ProductosStockBajo { get; set; }

    public List<PuntoVentaDiaria> VentasUltimos7Dias { get; set; } = new();
    public List<Producto> ProductosConStockBajo { get; set; } = new();
    public List<Venta> UltimasVentas { get; set; } = new();
    public List<Producto> ProximosVencimientos { get; set; } = new();
}

public class PuntoVentaDiaria
{
    public string Dia { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
