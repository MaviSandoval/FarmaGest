using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class CajaService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public CajaService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<Caja> AbrirCajaAsync(int usuarioId, decimal montoInicial)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cajaAbierta = await context.Cajas
            .AnyAsync(c => c.UsuarioId == usuarioId && c.Estado);

        if (cajaAbierta)
            throw new InvalidOperationException("El usuario ya tiene una caja abierta.");

        var caja = new Caja
        {
            FechaApertura = DateTime.Now,
            MontoInicial = montoInicial,
            Estado = true,
            UsuarioId = usuarioId
        };

        context.Cajas.Add(caja);
        await context.SaveChangesAsync();

        return caja;
    }

    public async Task<Caja?> ObtenerCajaAbiertaAsync(int usuarioId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Cajas
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Estado);
    }

    public async Task CerrarCajaAsync(int usuarioId, decimal montoFinal)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var caja = await context.Cajas
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Estado);

        if (caja == null)
            throw new InvalidOperationException("El usuario no tiene una caja abierta.");

        caja.FechaCierre = DateTime.Now;
        caja.MontoFinal = montoFinal;
        caja.Estado = false;

        await context.SaveChangesAsync();
    }

    public async Task<decimal> ObtenerTotalVentasAsync(int cajaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.DetallesVenta
            .Where(d => d.Venta.CajaId == cajaId && d.Venta.Estado)
            .SumAsync(d => (decimal?)((d.Cantidad * d.PrecioUnitario) - d.Descuento))
            ?? 0;
    }

    public async Task<decimal> ObtenerMontoEsperadoAsync(int cajaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var caja = await context.Cajas
            .FirstOrDefaultAsync(c => c.Id == cajaId);

        if (caja == null)
            throw new InvalidOperationException("Caja no encontrada.");

        var totalVentas = await context.DetallesVenta
            .Where(d => d.Venta.CajaId == cajaId && d.Venta.Estado)
            .SumAsync(d => (decimal?)((d.Cantidad * d.PrecioUnitario) - d.Descuento))
            ?? 0;

        return caja.MontoInicial + totalVentas;
    }
}