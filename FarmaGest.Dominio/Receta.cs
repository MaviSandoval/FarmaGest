namespace FarmaGest.Dominio;

/// <summary>
/// Receta recibida desde la obra social (se carga en la base de datos).
/// Estado (columna BIT que acepta NULL):
///   NULL  = Pendiente (todavía no la revisó el Farmacéutico)
///   true  = Validada  (se puede usar en una venta)
///   false = Rechazada
/// </summary>
public class Receta
{
    // Textos que se muestran en pantalla
    public const string Pendiente = "Pendiente";
    public const string Validada = "Validada";
    public const string Rechazada = "Rechazada";

    public int Id { get; set; }
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string MatriculaMedico { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
    public bool? Estado { get; set; }

    public int AfiliadoId { get; set; }
    public Afiliado Afiliado { get; set; } = null!;

    public List<DetalleReceta> Detalles { get; set; } = new();

    /// <summary>Convierte el valor de la columna estado en el texto que se muestra.</summary>
    public static string TextoEstado(bool? estado) =>
        estado == null ? Pendiente : estado.Value ? Validada : Rechazada;
}

public class DetalleReceta
{
    public int Id { get; set; }
    public int Cantidad { get; set; }

    public int RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
}
