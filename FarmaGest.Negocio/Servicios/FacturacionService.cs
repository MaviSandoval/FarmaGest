using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class FacturacionService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public FacturacionService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================================
    // OBTENER VENTAS PENDIENTES DE FACTURACIÓN
    // =========================================================
    public async Task<List<VentaPendienteFacturacionDto>>
        ObtenerVentasPendientesAsync()
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v =>
                v.Estado &&
                !context.Facturaciones.Any(f => f.VentaId == v.Id))
            .Select(v => new VentaPendienteFacturacionDto
            {
                VentaId = v.Id,
                Fecha = v.Fecha,

                Responsable =
                    v.Usuario.Nombre + " " + v.Usuario.Apellido,

                CantidadProductos =
                    v.Detalles.Sum(d => d.Cantidad),

                Total =
                    v.Detalles.Sum(d =>
                        (d.Cantidad * d.PrecioUnitario)
                        - d.Descuento)
            })
            .OrderBy(v => v.Fecha)
            .ToListAsync();

        return ventas;
    }

    // =========================================================
    // FACTURAR / COBRAR UNA VENTA
    // =========================================================
    public async Task FacturarVentaAsync(
        int ventaId,
        int usuarioId,
        string metodoPago)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        // 1. Buscar la venta
        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null)
            throw new InvalidOperationException(
                "La venta no existe.");

        if (!venta.Estado)
            throw new InvalidOperationException(
                "La venta no se encuentra activa.");

        // 2. Verificar que todavía no esté facturada
        var yaFacturada = await context.Facturaciones
            .AnyAsync(f => f.VentaId == ventaId);

        if (yaFacturada)
            throw new InvalidOperationException(
                "La venta ya fue facturada.");

        // 3. Buscar la caja abierta del cajero
        var caja = await context.Cajas
            .FirstOrDefaultAsync(c =>
                c.UsuarioId == usuarioId &&
                c.Estado);

        if (caja == null)
            throw new InvalidOperationException(
                "Debe abrir una caja antes de realizar el cobro.");

        // 4. Validar método de pago
        if (string.IsNullOrWhiteSpace(metodoPago))
            throw new InvalidOperationException(
                "Debe seleccionar un método de pago.");

        // 5. Calcular el importe total de la venta
        var importe = venta.Detalles.Sum(d =>
            (d.Cantidad * d.PrecioUnitario)
            - d.Descuento);

        if (importe <= 0)
            throw new InvalidOperationException(
                "La venta no posee un importe válido.");

        // 6. Crear la facturación
        var facturacion = new Facturacion
        {
            Fecha = DateTime.Now,
            MetodoPago = metodoPago,
            Importe = importe,
            VentaId = venta.Id,
            UsuarioId = usuarioId,
            CajaId = caja.Id
        };

        context.Facturaciones.Add(facturacion);

        await context.SaveChangesAsync();
    }
}