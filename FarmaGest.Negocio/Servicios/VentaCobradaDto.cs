namespace FarmaGest.Negocio.Servicios;

public class VentaCobradaDto
{
    public int VentaId { get; set; }

    public DateTime Fecha { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public string MetodoPago { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public string Cajero { get; set; } = string.Empty;

    public string Hora => Fecha.ToString("HH:mm");
}