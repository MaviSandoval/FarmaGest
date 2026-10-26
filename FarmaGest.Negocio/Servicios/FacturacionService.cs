using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class FacturacionService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public FacturacionService(
        IDbContextFactory<FarmaGestDbContext> factory)
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
                !context.Facturaciones.Any(
                    f => f.VentaId == v.Id))
            .Select(v => new VentaPendienteFacturacionDto
            {
                VentaId = v.Id,

                Fecha = v.Fecha,

                Responsable =
                    v.Usuario.Nombre + " " +
                    v.Usuario.Apellido,

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
    // OBTENER VENTAS COBRADAS
    // =========================================================
    public async Task<List<VentaCobradaDto>>
        ObtenerVentasCobradasAsync()
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var ventas = await context.Facturaciones
            .AsNoTracking()
            .Where(f => f.Venta.Estado)
            .Select(f => new VentaCobradaDto
            {
                VentaId = f.VentaId,

                Fecha = f.Fecha,

                // Usuario que realizó/preparó la venta.
                Responsable =
                    f.Venta.Usuario.Nombre + " " +
                    f.Venta.Usuario.Apellido,

                MetodoPago = f.MetodoPago,

                Importe = f.Importe,

                // Usuario que realizó el cobro.
                Cajero =
                    f.Usuario.Nombre + " " +
                    f.Usuario.Apellido
            })
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        return ventas;
    }

    // =========================================================
    // OBTENER RESUMEN DE LA CAJA ABIERTA
    // =========================================================
    public async Task<ResumenCajaDto?>
        ObtenerResumenCajaAbiertaAsync(int usuarioId)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        // -----------------------------------------------------
        // 1. Buscar la caja abierta del cajero
        // -----------------------------------------------------

        var caja = await context.Cajas
            .AsNoTracking()
            .Where(c =>
                c.UsuarioId == usuarioId &&
                c.Estado)
            .Select(c => new
            {
                c.Id,
                c.FechaApertura,
                c.MontoInicial,

                Cajero =
                    c.Usuario.Nombre + " " +
                    c.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (caja == null)
            return null;

        // -----------------------------------------------------
        // 2. Obtener todos los cobros realizados en esa caja
        // -----------------------------------------------------

        var cobros = await context.Facturaciones
            .AsNoTracking()
            .Where(f =>
                f.CajaId == caja.Id &&
                f.Venta.Estado)
            .Select(f => new MovimientoCajaDto
            {
                FacturacionId = f.Id,

                VentaId = f.VentaId,

                Fecha = f.Fecha,

                // Usuario que realizó/preparó originalmente la venta.
                Responsable =
                    f.Venta.Usuario.Nombre + " " +
                    f.Venta.Usuario.Apellido,

                MetodoPago = f.MetodoPago,

                Importe = f.Importe
            })
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        // -----------------------------------------------------
        // 3. Calcular totales
        // -----------------------------------------------------

        decimal totalCobrado =
            cobros.Sum(c => c.Importe);

        decimal totalEfectivo =
            cobros
                .Where(c =>
                    c.MetodoPago.Equals(
                        "Efectivo",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.Importe);

        decimal efectivoEsperado =
            caja.MontoInicial + totalEfectivo;

        // -----------------------------------------------------
        // 4. Agrupar cobros por medio de pago
        // -----------------------------------------------------

        var mediosPago = cobros
            .GroupBy(c => c.MetodoPago)
            .Select(g => new MedioPagoResumenDto
            {
                Medio = g.Key,

                Cantidad = g.Count(),

                Importe = g.Sum(c => c.Importe)
            })
            .OrderBy(m => m.Medio)
            .ToList();

        // -----------------------------------------------------
        // 5. Armar resumen
        // -----------------------------------------------------

        return new ResumenCajaDto
        {
            CajaId = caja.Id,

            FechaApertura = caja.FechaApertura,

            Cajero = caja.Cajero,

            MontoInicial = caja.MontoInicial,

            TotalCobrado = totalCobrado,

            TotalEfectivo = totalEfectivo,

            EfectivoEsperado = efectivoEsperado,

            CantidadCobros = cobros.Count,

            MediosPago = mediosPago,

            Movimientos = cobros
        };
    }

    // =========================================================
    // TICKET DE UNA VENTA COBRADA
    // Cruza Facturacion, Venta, DetalleVenta, Producto, Usuario
    // (quien preparó y quien cobró) y Receta/Afiliado/Plan/ObraSocial.
    // =========================================================
    public async Task<TicketDto?> ObtenerTicketAsync(int ventaId)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var ticket = await context.Facturaciones
            .AsNoTracking()
            .Where(f => f.VentaId == ventaId)
            .Select(f => new TicketDto
            {
                VentaId = f.VentaId,
                FacturacionId = f.Id,
                Fecha = f.Fecha,
                Preparo = f.Venta.Usuario.Nombre + " " + f.Venta.Usuario.Apellido,
                Cobro = f.Usuario.Nombre + " " + f.Usuario.Apellido,
                MetodoPago = f.MetodoPago,
                Importe = f.Importe,
                MontoCambio = f.MontoCambio,
                ObraSocial = f.Venta.Receta == null ? null : f.Venta.Receta.Afiliado.Plan.ObraSocial.Nombre,
                Plan = f.Venta.Receta == null ? null : f.Venta.Receta.Afiliado.Plan.NombrePlan,
                Afiliado = f.Venta.Receta == null ? null
                    : f.Venta.Receta.Afiliado.Nombre + " " + f.Venta.Receta.Afiliado.Apellido
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
            return null;

        ticket.Items = await context.DetallesVenta
            .AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .OrderBy(d => d.Id)
            .Select(d => new ItemTicketDto
            {
                Producto = d.Producto.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento
            })
            .ToListAsync();

        return ticket;
    }

    // =========================================================
    // FACTURAR / COBRAR UNA VENTA
    // =========================================================
    public async Task FacturarVentaAsync(
        int ventaId,
        int usuarioId,
        string metodoPago,
        decimal montoCambio)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        // -----------------------------------------------------
        // 1. Buscar la venta
        // -----------------------------------------------------

        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(
                v => v.Id == ventaId);

        if (venta == null)
            throw new InvalidOperationException(
                "La venta no existe.");

        if (!venta.Estado)
            throw new InvalidOperationException(
                "La venta no se encuentra activa.");

        // -----------------------------------------------------
        // 2. Verificar que todavía no esté facturada
        // -----------------------------------------------------

        var yaFacturada =
            await context.Facturaciones
                .AnyAsync(
                    f => f.VentaId == ventaId);

        if (yaFacturada)
            throw new InvalidOperationException(
                "La venta ya fue facturada.");

        // -----------------------------------------------------
        // 3. Buscar la caja abierta del cajero
        // -----------------------------------------------------

        var caja = await context.Cajas
            .FirstOrDefaultAsync(c =>
                c.UsuarioId == usuarioId &&
                c.Estado);

        if (caja == null)
            throw new InvalidOperationException(
                "Debe abrir una caja antes de realizar el cobro.");

        // -----------------------------------------------------
        // 4. Validar método de pago
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(metodoPago))
            throw new InvalidOperationException(
                "Debe seleccionar un método de pago.");

        // -----------------------------------------------------
        // 5. Calcular importe
        // -----------------------------------------------------

        var importe =
            venta.Detalles.Sum(d =>
                (d.Cantidad * d.PrecioUnitario)
                - d.Descuento);

        if (importe <= 0)
            throw new InvalidOperationException(
                "La venta no posee un importe válido.");

        // -----------------------------------------------------
        // 6. Validar monto de cambio
        // -----------------------------------------------------

        if (montoCambio < 0)
            throw new InvalidOperationException(
                "El monto de cambio no puede ser negativo.");

        // Si el pago no es en efectivo, no corresponde vuelto.
        if (!metodoPago.Trim().Equals(
                "Efectivo",
                StringComparison.OrdinalIgnoreCase))
        {
            montoCambio = 0;
        }

        // -----------------------------------------------------
        // 7. Crear facturación
        // -----------------------------------------------------

        var facturacion =
            new Facturacion
            {
                Fecha = DateTime.Now,

                MetodoPago =
                    metodoPago.Trim(),

                Importe = importe,

                MontoCambio = montoCambio,

                VentaId = venta.Id,

                // Cajero que realizó el cobro
                UsuarioId = usuarioId,

                CajaId = caja.Id
            };

        context.Facturaciones.Add(
            facturacion);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos cajeros cobraron la misma venta a la vez: el índice único
            // de Facturacion.id_venta deja pasar solo al primero.
            if (await context.Facturaciones.AsNoTracking().AnyAsync(f => f.VentaId == ventaId && f.Id != facturacion.Id))
                throw new InvalidOperationException(
                    "Otro cajero acaba de cobrar esta venta. Actualizá la lista de preventas.");

            throw;
        }
    }
}


// =============================================================
// DTO - TICKET DE UNA VENTA COBRADA
// =============================================================

public class TicketDto
{
    public int VentaId { get; set; }
    public int FacturacionId { get; set; }
    public DateTime Fecha { get; set; }
    public string Preparo { get; set; } = string.Empty;
    public string Cobro { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public decimal MontoCambio { get; set; }
    public string? ObraSocial { get; set; }
    public string? Plan { get; set; }
    public string? Afiliado { get; set; }

    public List<ItemTicketDto> Items { get; set; } = new();

    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public decimal Descuento => Items.Sum(i => i.Descuento);
    public decimal Total => Subtotal - Descuento;

    public bool EsEfectivo => MetodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase);

    /// <summary>Lo que entregó el cliente: importe + vuelto.</summary>
    public decimal PagaCon => Importe + MontoCambio;

    public string NumeroTicket => $"0001-{FacturacionId:D8}";
}

public class ItemTicketDto
{
    private static readonly System.Globalization.CultureInfo Cultura = new("es-AR");

    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
    public decimal PorcentajeCobertura => Subtotal == 0 ? 0 : Math.Round(Descuento * 100 / Subtotal, 0);
    public bool TieneDescuento => Descuento > 0;
    public string Linea => $"{Cantidad} x {PrecioUnitario.ToString("N2", Cultura)}";
    public string TextoDescuento => $"  Cobertura {PorcentajeCobertura:0}%";
}


// =============================================================
// DTO - RESUMEN DE CAJA
// =============================================================

public class ResumenCajaDto
{
    public int CajaId { get; set; }

    public DateTime FechaApertura { get; set; }

    public string Cajero { get; set; } =
        string.Empty;

    public decimal MontoInicial { get; set; }

    public decimal TotalCobrado { get; set; }

    public decimal TotalEfectivo { get; set; }

    public decimal EfectivoEsperado { get; set; }

    public int CantidadCobros { get; set; }

    public List<MedioPagoResumenDto>
        MediosPago
    { get; set; } = new();

    public List<MovimientoCajaDto>
        Movimientos
    { get; set; } = new();
}


// =============================================================
// DTO - TOTAL POR MEDIO DE PAGO
// =============================================================

public class MedioPagoResumenDto
{
    public string Medio { get; set; } =
        string.Empty;

    public int Cantidad { get; set; }

    public decimal Importe { get; set; }
}


// =============================================================
// DTO - MOVIMIENTO DE CAJA
// =============================================================

public class MovimientoCajaDto
{
    public int FacturacionId { get; set; }

    public int VentaId { get; set; }

    public DateTime Fecha { get; set; }

    public string Responsable { get; set; } =
        string.Empty;

    public string MetodoPago { get; set; } =
        string.Empty;

    public decimal Importe { get; set; }
}