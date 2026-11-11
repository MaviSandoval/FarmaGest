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
                FacturacionId = f.Id,

                VentaId = f.VentaId,

                Fecha = f.Fecha,

                Responsable =
                    f.Venta.Usuario.Nombre + " " +
                    f.Venta.Usuario.Apellido,

                MetodoPago =
                    f.MedioPago.Nombre,

                Importe =
                    f.Importe,

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

    public Task<ResumenCajaDto?> ObtenerResumenCajaAbiertaAsync(int usuarioId)
        => ObtenerResumenAsync(usuarioId, true);

    public Task<ResumenCajaDto?> ObtenerResumenCajaAsync(int usuarioId)
        => ObtenerResumenAsync(usuarioId, false);

    private async Task<ResumenCajaDto?> ObtenerResumenAsync(int usuarioId, bool soloAbierta)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        var caja = await context.Cajas
            .AsNoTracking()
            .Where(c =>
                c.UsuarioId == usuarioId &&
                (!soloAbierta || c.Estado))
            .OrderByDescending(c => c.Estado)
            .ThenByDescending(c => c.FechaCierre)
            .ThenByDescending(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.FechaApertura,
                c.MontoInicial,
                c.MontoFinal,
                c.FechaCierre,
                c.Estado,

                Cajero =
                    c.Usuario.Nombre + " " +
                    c.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (caja == null)
            return null;

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

                Responsable =
                    f.Venta.Usuario.Nombre + " " +
                    f.Venta.Usuario.Apellido,

                MetodoPago =
                    f.MedioPago.Nombre,

                Importe =
                    f.Importe
            })
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        decimal totalCobrado =
            cobros.Sum(c => c.Importe);

        decimal totalEfectivo =
            cobros
                .Where(c =>
                    CalculosCaja.EsEfectivo(c.MetodoPago))
                .Sum(c => c.Importe);

        decimal efectivoEsperado =
            CalculosCaja.EfectivoEsperado(caja.MontoInicial, totalEfectivo);

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

        return new ResumenCajaDto
        {
            CajaId = caja.Id,
            FechaApertura = caja.FechaApertura,
            Cajero = caja.Cajero,
            MontoInicial = caja.MontoInicial,
            MontoFinal = caja.MontoFinal,
            FechaCierre = caja.FechaCierre,
            Estado = caja.Estado,
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
    // =========================================================

    public async Task<TicketDto?>
        ObtenerTicketAsync(int ventaId)
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

                Preparo =
                    f.Venta.Usuario.Nombre + " " +
                    f.Venta.Usuario.Apellido,

                Cobro =
                    f.Usuario.Nombre + " " +
                    f.Usuario.Apellido,

                MetodoPago = f.MetodoPago,

                Importe = f.Importe,

                MontoCambio = f.MontoCambio,

                TitularTarjeta = f.DetallePagoTarjeta == null ? null : f.DetallePagoTarjeta.Titular,
                Ultimos4Tarjeta = f.DetallePagoTarjeta == null ? null : f.DetallePagoTarjeta.Ultimos4,
                ReferenciaSimulacion = f.DetallePagoTarjeta == null ? null : f.DetallePagoTarjeta.NumeroAutorizacion,
                Cuotas = f.DetallePagoTarjeta == null ? null : (int?)f.DetallePagoTarjeta.Cuotas,
                NumeroOperacion = f.DetalleTransferencia == null ? null : f.DetalleTransferencia.NumeroOperacion,

                ObraSocial =
                    f.Venta.Receta == null
                        ? null
                        : f.Venta.Receta.Afiliado
                            .Plan.ObraSocial.Nombre,

                Plan =
                    f.Venta.Receta == null
                        ? null
                        : f.Venta.Receta.Afiliado
                            .Plan.NombrePlan,

                Afiliado =
                    f.Venta.Receta == null
                        ? null
                        : f.Venta.Receta.Afiliado.Nombre +
                          " " +
                          f.Venta.Receta.Afiliado.Apellido
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
    // OBTENER MEDIOS DE PAGO ACTIVOS
    // =========================================================

    public async Task<List<MedioPago>>
        ObtenerMediosPagoActivosAsync()
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        return await context.MediosPago
            .AsNoTracking()
            .Where(m => m.Estado)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    // =========================================================
    // FACTURAR / COBRAR UNA VENTA
    // =========================================================

    public async Task<string> ValidarNumeroOperacionTransferenciaAsync(string? numeroOperacion)
    {
        var numero = ValidacionTransferencia.Normalizar(numeroOperacion);
        await using var context = await _factory.CreateDbContextAsync();
        await VerificarOperacionDisponibleAsync(context, numero);
        return numero;
    }

    private static async Task VerificarOperacionDisponibleAsync(FarmaGestDbContext context, string numero)
    {
        // Collation explícita para no depender de si la base distingue mayúsculas.
        if (await context.DetallesTransferencia.AsNoTracking().AnyAsync(d =>
            EF.Functions.Collate(d.NumeroOperacion.Trim(), "Latin1_General_100_CI_AS") == numero))
            throw new InvalidOperationException(ValidacionTransferencia.MensajeDuplicado);
    }

    public async Task FacturarVentaAsync(
        int ventaId,
        int usuarioId,
        string metodoPago,
        decimal montoCambio,
        DatosTarjetaSolicitud? tarjeta = null,
        string? numeroOperacion = null)
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

        var medioPago = await context.MediosPago
            .FirstOrDefaultAsync(m =>
                m.Estado &&
                m.Nombre == metodoPago.Trim());

        if (medioPago == null)
            throw new InvalidOperationException(
                "El método de pago seleccionado no existe o está inactivo.");

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

        if (!medioPago.Nombre.Equals(
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

                // Se mantiene temporalmente mientras
                // finalizamos la migración.
                MetodoPago = medioPago.Nombre,

                MedioPagoId = medioPago.Id,

                Importe = importe,

                MontoCambio = montoCambio,

                VentaId = venta.Id,

                UsuarioId = usuarioId,

                CajaId = caja.Id
            };

        if (medioPago.Nombre.Equals("Débito", StringComparison.OrdinalIgnoreCase) ||
            medioPago.Nombre.Equals("Crédito", StringComparison.OrdinalIgnoreCase))
        {
            if (tarjeta == null || string.IsNullOrWhiteSpace(tarjeta.Titular) ||
                tarjeta.Titular.Trim().Length > 100 || tarjeta.Ultimos4 == null ||
                tarjeta.Ultimos4.Length != 4 || tarjeta.Ultimos4.Any(c => c < '0' || c > '9'))
                throw new InvalidOperationException("Ingrese titular y los últimos cuatro dígitos de la tarjeta.");

            bool debito = medioPago.Nombre.Equals("Débito", StringComparison.OrdinalIgnoreCase);
            if ((debito && tarjeta.Cuotas != 1) || (!debito && tarjeta.Cuotas is not (1 or 3 or 6 or 12)))
                throw new InvalidOperationException("La cantidad de cuotas no es válida para el medio de pago.");
            if (numeroOperacion != null)
                throw new InvalidOperationException("El pago con tarjeta no debe incluir datos de transferencia.");

            facturacion.DetallePagoTarjeta = new DetallePagoTarjeta
            {
                Titular = tarjeta.Titular.Trim(),
                Ultimos4 = tarjeta.Ultimos4,
                Cuotas = tarjeta.Cuotas,
                // La columna conserva su nombre por compatibilidad con el esquema existente.
                NumeroAutorizacion = $"SIM-{Guid.NewGuid():N}"
            };
        }
        else if (medioPago.Nombre.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
        {
            numeroOperacion = ValidacionTransferencia.Normalizar(numeroOperacion);
            if (tarjeta != null)
                throw new InvalidOperationException("La transferencia no debe incluir datos de tarjeta.");
            facturacion.DetalleTransferencia = new DetalleTransferencia
            {
                NumeroOperacion = numeroOperacion
            };
        }
        else if (tarjeta != null || numeroOperacion != null)
        {
            throw new InvalidOperationException("Los detalles no corresponden al medio de pago seleccionado.");
        }

        context.Facturaciones.Add(
            facturacion);

        // Solo las transferencias necesitan exclusión entre la consulta y el INSERT.
        await using var transaccion = facturacion.DetalleTransferencia != null
            ? await context.Database.BeginTransactionAsync()
            : null;
        try
        {
            if (transaccion != null)
            {
                // Bloqueo compartido por todas las instancias de FarmaGest en esta BD.
                // Se libera al confirmar/revertir la transacción. No modifica el esquema.
                await context.Database.ExecuteSqlRawAsync(@"
DECLARE @resultado int;
EXEC @resultado = sys.sp_getapplock
    @Resource = N'FarmaGest.Transferencias.Registro',
    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
IF @resultado < 0
    THROW 51000, 'No se pudo validar la transferencia por concurrencia. Intente nuevamente.', 1;");
                await VerificarOperacionDisponibleAsync(context, numeroOperacion!);
            }
            // La facturación y su detalle se confirman o revierten juntos.
            await context.SaveChangesAsync();
            if (transaccion != null) await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            if (transaccion != null) await transaccion.RollbackAsync();
            if (await context.Facturaciones
                .AsNoTracking()
                .AnyAsync(f =>
                    f.VentaId == ventaId &&
                    f.Id != facturacion.Id))
            {
                throw new InvalidOperationException(
                    "Otro cajero acaba de cobrar esta venta. Actualizá la lista de preventas.");
            }

            throw;
        }
    }
}


// =============================================================
// DTO - TICKET DE UNA VENTA COBRADA
// =============================================================

public class TicketDto
{
    public string? TitularTarjeta { get; set; }
    public string? Ultimos4Tarjeta { get; set; }
    public string? ReferenciaSimulacion { get; set; }
    public int? Cuotas { get; set; }
    public string? NumeroOperacion { get; set; }

    public bool EsTarjeta => MetodoPago.Equals("Débito", StringComparison.OrdinalIgnoreCase) ||
        MetodoPago.Equals("Crédito", StringComparison.OrdinalIgnoreCase);
    public bool EsTransferencia => MetodoPago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase);

    public int VentaId { get; set; }

    public int FacturacionId { get; set; }

    public DateTime Fecha { get; set; }

    public string Preparo { get; set; } =
        string.Empty;

    public string Cobro { get; set; } =
        string.Empty;

    public string MetodoPago { get; set; } =
        string.Empty;

    public decimal Importe { get; set; }

    public decimal MontoCambio { get; set; }

    public string? ObraSocial { get; set; }

    public string? Plan { get; set; }

    public string? Afiliado { get; set; }

    public List<ItemTicketDto> Items { get; set; } =
        new();

    public decimal Subtotal =>
        Items.Sum(i => i.Subtotal);

    public decimal Descuento =>
        Items.Sum(i => i.Descuento);

    public decimal Total =>
        Subtotal - Descuento;

    public bool EsEfectivo =>
        MetodoPago.Equals(
            "Efectivo",
            StringComparison.OrdinalIgnoreCase);

    public decimal PagaCon =>
        Importe + MontoCambio;

    public string NumeroTicket =>
        $"0001-{FacturacionId:D8}";
}


public class ItemTicketDto
{
    private static readonly
        System.Globalization.CultureInfo Cultura =
            new("es-AR");

    public string Producto { get; set; } =
        string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Subtotal =>
        Cantidad * PrecioUnitario;

    public decimal PorcentajeCobertura =>
        Subtotal == 0
            ? 0
            : Math.Round(
                Descuento * 100 / Subtotal,
                0);

    public bool TieneDescuento =>
        Descuento > 0;

    public string Linea =>
        $"{Cantidad} x " +
        $"{PrecioUnitario.ToString("N2", Cultura)}";

    public string TextoDescuento =>
        $"  Cobertura {PorcentajeCobertura:0}%";
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

    public decimal? MontoFinal { get; set; }

    public bool Estado { get; set; }

    public DateTime? FechaCierre { get; set; }

    public int CantidadEfectivo => Movimientos.Count(c => CalculosCaja.EsEfectivo(c.MetodoPago));

    public decimal TotalOtrosMedios => TotalCobrado - TotalEfectivo;

    public decimal TicketPromedio => CantidadCobros == 0 ? 0 : TotalCobrado / CantidadCobros;

    public decimal? VentaMayorImporte => Movimientos.Count == 0 ? null : Movimientos.Max(m => m.Importe);

    public decimal? Diferencia => Estado ? null : MontoFinal - EfectivoEsperado;

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
