namespace FarmaGest.Negocio.Servicios;

// Solo los datos que se conservan en el comprobante del pago simulado.
public record DatosTarjetaSolicitud(string Titular, string Ultimos4, int Cuotas);
