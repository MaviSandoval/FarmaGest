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
    public List<ProductoStockBajoResumen> ProductosConStockBajo { get; set; } = new();
    public List<UltimaVentaResumen> UltimasVentas { get; set; } = new();
    // La base no guarda vencimientos por lote: la lista queda vacía
    public List<VencimientoResumen> ProximosVencimientos { get; set; } = new();
}

public class PuntoVentaDiaria
{
    public string Dia { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ProductoStockBajoResumen
{
    public string Descripcion { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
}

public class UltimaVentaResumen
{
    public int NumeroVenta { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime Fecha { get; set; }
}

public class VencimientoResumen
{
    public string Descripcion { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int Cantidad { get; set; }
}
