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

    public async Task<Caja> AbrirCajaAsync(
        int usuarioId,
        decimal montoInicial)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var cajaAbierta = await context.Cajas
            .AnyAsync(c =>
                c.UsuarioId == usuarioId &&
                c.Estado);

        if (cajaAbierta)
            throw new InvalidOperationException(
                "El usuario ya tiene una caja abierta.");

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

    public async Task<Caja?> ObtenerCajaAbiertaAsync(
        int usuarioId)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        return await context.Cajas
            .FirstOrDefaultAsync(c =>
                c.UsuarioId == usuarioId &&
                c.Estado);
    }

    public async Task CerrarCajaAsync(
        int usuarioId,
        decimal montoFinal)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var caja = await context.Cajas
            .FirstOrDefaultAsync(c =>
                c.UsuarioId == usuarioId &&
                c.Estado);

        if (caja == null)
            throw new InvalidOperationException(
                "El usuario no tiene una caja abierta.");

        caja.FechaCierre = DateTime.Now;
        caja.MontoFinal = montoFinal;
        caja.Estado = false;

        await context.SaveChangesAsync();
    }

    public async Task<ResumenCajaDto> ObtenerTotalesAsync(int cajaId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var caja = await context.Cajas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cajaId)
            ?? throw new InvalidOperationException("Caja no encontrada.");
        var cobros = await context.Facturaciones.AsNoTracking()
            .Where(f => f.CajaId == cajaId && f.Venta.Estado)
            .Select(f => new MovimientoCajaDto { MetodoPago = f.MedioPago.Nombre, Importe = f.Importe })
            .ToListAsync();
        var efectivo = cobros.Where(c => CalculosCaja.EsEfectivo(c.MetodoPago)).Sum(c => c.Importe);
        return new ResumenCajaDto
        {
            TotalCobrado = cobros.Sum(c => c.Importe),
            TotalEfectivo = efectivo,
            EfectivoEsperado = CalculosCaja.EfectivoEsperado(caja.MontoInicial, efectivo),
            CantidadCobros = cobros.Count,
            Movimientos = cobros
        };
    }

    public async Task<decimal> ObtenerTotalVentasAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).TotalCobrado;

    public async Task<decimal> ObtenerTotalEfectivoAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).TotalEfectivo;

    public async Task<int> ObtenerCantidadCobrosEfectivoAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).Movimientos.Count(c => CalculosCaja.EsEfectivo(c.MetodoPago));

    public async Task<decimal> ObtenerTotalOtrosMediosAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).TotalOtrosMedios;

    public async Task<int> ObtenerCantidadOtrosMediosAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).Movimientos.Count(c => !CalculosCaja.EsEfectivo(c.MetodoPago));

    public async Task<decimal> ObtenerMontoEsperadoAsync(int cajaId)
        => (await ObtenerTotalesAsync(cajaId)).EfectivoEsperado;
}