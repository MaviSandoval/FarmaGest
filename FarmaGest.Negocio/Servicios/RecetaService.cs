using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Recetas recibidas desde las obras sociales (se cargan en la base de datos).
/// El Farmacéutico las valida o las rechaza; el Administrador solo las consulta.
/// Estado: NULL = Pendiente, 1 = Validada, 0 = Rechazada.
/// </summary>
public class RecetaService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public RecetaService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Todas las recetas con afiliado, plan, medicamentos, coberturas y verificaciones.</summary>
    public async Task<List<RecetaDto>> ObtenerRecetasAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var recetas = await context.Recetas
            .AsNoTracking()
            .Include(r => r.Afiliado).ThenInclude(a => a.Plan).ThenInclude(p => p.ObraSocial)
            .Include(r => r.Afiliado).ThenInclude(a => a.Plan).ThenInclude(p => p.Coberturas)
            .Include(r => r.Detalles).ThenInclude(d => d.Producto)
            .AsSplitQuery()
            .OrderByDescending(r => r.Id)
            .ToListAsync();

        // Recetas que ya se usaron en una venta que no está anulada: receta -> N° de venta
        var usadas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado && v.RecetaId != null)
            .Select(v => new { RecetaId = v.RecetaId!.Value, v.Id })
            .ToListAsync();

        var ventaPorReceta = usadas
            .GroupBy(u => u.RecetaId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        return recetas
            .Select(r => ArmarDto(r, ventaPorReceta.TryGetValue(r.Id, out var ventaId) ? ventaId : null))
            .ToList();
    }

    /// <summary>Recetas que se pueden usar para armar una venta ahora.</summary>
    public async Task<List<RecetaDto>> ObtenerValidadasDisponiblesAsync()
    {
        var recetas = await ObtenerRecetasAsync();
        return recetas
            .Where(r => r.PuedeIniciarVenta)
            .OrderBy(r => r.FechaVencimiento)
            .ToList();
    }

    public async Task ValidarAsync(int recetaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var receta = await context.Recetas
            .Include(r => r.Afiliado)
            .FirstOrDefaultAsync(r => r.Id == recetaId)
            ?? throw new InvalidOperationException("Receta no encontrada.");

        if (receta.Estado != null)
            throw new InvalidOperationException($"La receta ya está {Receta.TextoEstado(receta.Estado).ToLower()}.");

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        if (receta.FechaVencimiento < hoy)
            throw new InvalidOperationException("La receta está vencida: no se puede validar. Rechazala.");
        if (receta.FechaEmision > hoy)
            throw new InvalidOperationException("La fecha de emisión es posterior a hoy: revisá la receta.");
        if (!receta.Afiliado.Activo)
            throw new InvalidOperationException("El afiliado no está activo en su obra social: no se puede validar.");

        receta.Estado = true;   // Validada
        await context.SaveChangesAsync();
    }

    public async Task RechazarAsync(int recetaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var receta = await context.Recetas.FindAsync(recetaId)
            ?? throw new InvalidOperationException("Receta no encontrada.");

        if (receta.Estado == false)
            throw new InvalidOperationException("La receta ya está rechazada.");

        if (await context.Ventas.AnyAsync(v => v.RecetaId == recetaId && v.Estado))
            throw new InvalidOperationException("La receta ya se usó en una venta: anulá la venta antes de rechazarla.");

        receta.Estado = false;  // Rechazada
        await context.SaveChangesAsync();
    }

    // =========================================================
    // ARMADO DEL DTO + VERIFICACIONES AUTOMÁTICAS
    // =========================================================
    private static RecetaDto ArmarDto(Receta r, int? ventaId)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var afiliado = r.Afiliado;
        var plan = afiliado.Plan;

        var medicamentos = r.Detalles
            .OrderBy(d => d.Producto.Descripcion)
            .Select(d =>
            {
                var cobertura = plan.Coberturas.FirstOrDefault(c => c.ProductoId == d.ProductoId);

                return new MedicamentoRecetaDto
                {
                    ProductoId = d.ProductoId,
                    Producto = d.Producto.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.Producto.PrecioVenta,
                    Stock = d.Producto.StockVenta,
                    ProductoActivo = d.Producto.Estado,
                    TieneCobertura = cobertura != null,
                    Porcentaje = cobertura?.PorcentajeCobertura ?? 0
                };
            })
            .ToList();

        bool vigente = r.FechaEmision <= hoy && r.FechaVencimiento >= hoy;

        var verificaciones = new List<VerificacionDto>
        {
            r.FechaVencimiento < hoy
                ? new VerificacionDto($"Receta vencida el {r.FechaVencimiento:dd/MM/yyyy}", false)
                : r.FechaEmision > hoy
                    ? new VerificacionDto("La fecha de emisión es posterior a hoy", false)
                    : new VerificacionDto($"Receta vigente hasta el {r.FechaVencimiento:dd/MM/yyyy}", true),

            afiliado.Activo
                ? new VerificacionDto("Afiliado activo", true)
                : new VerificacionDto("Afiliado inactivo en la obra social", false)
        };

        var sinCobertura = medicamentos.Where(m => !m.TieneCobertura).ToList();
        if (sinCobertura.Count == 0)
            verificaciones.Add(new VerificacionDto("Todos los medicamentos tienen cobertura", true));
        else
            verificaciones.AddRange(sinCobertura.Select(m => new VerificacionDto($"{m.Producto}: sin cobertura en el plan", false)));

        var sinStock = medicamentos.Where(m => !m.ProductoActivo || m.Stock < m.Cantidad).ToList();
        if (sinStock.Count == 0)
            verificaciones.Add(new VerificacionDto("Stock disponible", true));
        else
            verificaciones.AddRange(sinStock.Select(m => new VerificacionDto(
                m.ProductoActivo ? $"{m.Producto}: stock insuficiente ({m.Stock} u.)" : $"{m.Producto}: producto dado de baja", false)));

        if (ventaId != null)
            verificaciones.Add(new VerificacionDto($"Ya se usó en la venta N° {ventaId}", false));

        return new RecetaDto
        {
            Numero = r.Id,
            Paciente = $"{afiliado.Nombre} {afiliado.Apellido}",
            NumeroAfiliado = afiliado.Id,
            AfiliadoActivo = afiliado.Activo,
            ObraSocial = plan.ObraSocial.Nombre,
            Plan = plan.NombrePlan,
            Medico = r.NombreMedico,
            Matricula = r.MatriculaMedico,
            FechaEmision = r.FechaEmision.ToDateTime(TimeOnly.MinValue),
            FechaVencimiento = r.FechaVencimiento.ToDateTime(TimeOnly.MinValue),
            Estado = Receta.TextoEstado(r.Estado),
            Vigente = vigente,
            VentaId = ventaId,
            Medicamentos = medicamentos,
            Verificaciones = verificaciones
        };
    }
}

// =========================================================
// DTOs
// =========================================================

public class RecetaDto
{
    public int Numero { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public int NumeroAfiliado { get; set; }
    public bool AfiliadoActivo { get; set; }
    public string ObraSocial { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Vigente { get; set; }
    public int? VentaId { get; set; }
    public List<MedicamentoRecetaDto> Medicamentos { get; set; } = new();
    public List<VerificacionDto> Verificaciones { get; set; } = new();

    public string ObraSocialPlan => $"{ObraSocial} · {Plan}";
    public bool EsPendiente => Estado == Receta.Pendiente;
    public bool PuedeRechazar => Estado != Receta.Rechazada && VentaId == null;
    public bool PuedeIniciarVenta => Estado == Receta.Validada && Vigente && AfiliadoActivo && VentaId == null;
    public string UsoTexto => VentaId != null ? $"Venta N° {VentaId}" : "—";

    /// <summary>Texto que se muestra en el combo de Nueva venta.</summary>
    public string TextoCombo => $"N° {Numero} — {Paciente} ({ObraSocialPlan})";
}

public class MedicamentoRecetaDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int Stock { get; set; }
    public bool ProductoActivo { get; set; }
    public bool TieneCobertura { get; set; }
    public decimal Porcentaje { get; set; }

    public string CoberturaTexto => TieneCobertura ? $"{Porcentaje:0.##} %" : "Sin cobertura";
}

public class VerificacionDto
{
    public VerificacionDto(string texto, bool ok)
    {
        Texto = texto;
        Ok = ok;
    }

    public string Texto { get; }
    public bool Ok { get; }
}
