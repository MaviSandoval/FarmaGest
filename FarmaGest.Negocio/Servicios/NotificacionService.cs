using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Avisos del Farmacéutico (panel "Notificaciones" del encabezado):
///  - Productos sin stock y con stock bajo.
///  - Recetas nuevas pendientes de validar.
///  - Recetas validadas que vencen en los próximos días y todavía no se usaron.
///  - Recetas pendientes que ya vencieron (hay que rechazarlas).
/// Se calculan en el momento a partir de la base: no se guardan.
/// </summary>
public class NotificacionService
{
    public const string DestinoStock = "Stock";
    public const string DestinoRecetas = "Recetas";

    /// <summary>Días antes del vencimiento en que se avisa de una receta validada sin usar.</summary>
    private const int DiasAvisoVencimiento = 3;

    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public NotificacionService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<NotificacionDto>> ObtenerAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var limiteVencimiento = hoy.AddDays(DiasAvisoVencimiento);
        var avisos = new List<NotificacionDto>();

        // ---------- Stock ----------
        var productos = await context.Productos
            .AsNoTracking()
            .Where(p => p.Estado && p.StockVenta < EstadosStock.UmbralStockBajo)
            .OrderBy(p => p.StockVenta).ThenBy(p => p.Descripcion)
            .Select(p => new { p.Descripcion, p.StockVenta })
            .ToListAsync();

        var sinStock = productos.Where(p => p.StockVenta <= 0).Select(p => p.Descripcion).ToList();
        if (sinStock.Count > 0)
            avisos.Add(new NotificacionDto(NotificacionDto.Urgente,
                $"{sinStock.Count} producto(s) sin stock",
                Resumir(sinStock),
                DestinoStock, "Ver stock"));

        var stockBajo = productos.Where(p => p.StockVenta > 0).Select(p => $"{p.Descripcion} ({p.StockVenta} u.)").ToList();
        if (stockBajo.Count > 0)
            avisos.Add(new NotificacionDto(NotificacionDto.Atencion,
                $"{stockBajo.Count} producto(s) con stock bajo",
                Resumir(stockBajo),
                DestinoStock, "Ver stock"));

        // ---------- Recetas ----------
        var recetas = await context.Recetas
            .AsNoTracking()
            .Where(r => r.Estado == null || (r.Estado == true && r.FechaVencimiento >= hoy && r.FechaVencimiento <= limiteVencimiento))
            .Select(r => new
            {
                r.Id,
                r.Estado,
                r.FechaVencimiento,
                Paciente = r.Afiliado.Nombre + " " + r.Afiliado.Apellido,
                ObraSocial = r.Afiliado.Plan.ObraSocial.Nombre,
                Usada = context.Ventas.Any(v => v.RecetaId == r.Id && v.Estado)
            })
            .ToListAsync();

        var pendientes = recetas.Where(r => r.Estado == null && r.FechaVencimiento >= hoy).ToList();
        if (pendientes.Count > 0)
            avisos.Add(new NotificacionDto(NotificacionDto.Informacion,
                $"{pendientes.Count} receta(s) nueva(s) para validar",
                Resumir(pendientes.Select(r => $"N° {r.Id} · {r.Paciente} ({r.ObraSocial})").ToList()),
                DestinoRecetas, "Validar recetas"));

        var porVencer = recetas.Where(r => r.Estado == true && !r.Usada).ToList();
        if (porVencer.Count > 0)
            avisos.Add(new NotificacionDto(NotificacionDto.Atencion,
                $"{porVencer.Count} receta(s) validada(s) vencen pronto",
                Resumir(porVencer.Select(r => $"N° {r.Id} · {r.Paciente} (vence {r.FechaVencimiento:dd/MM})").ToList()),
                DestinoRecetas, "Ver recetas"));

        var vencidasSinValidar = recetas.Where(r => r.Estado == null && r.FechaVencimiento < hoy).ToList();
        if (vencidasSinValidar.Count > 0)
            avisos.Add(new NotificacionDto(NotificacionDto.Urgente,
                $"{vencidasSinValidar.Count} receta(s) vencida(s) sin validar",
                "Ya no se pueden usar: revisalas y rechazalas. " +
                    Resumir(vencidasSinValidar.Select(r => $"N° {r.Id} · {r.Paciente}").ToList()),
                DestinoRecetas, "Ver recetas"));

        // Lo urgente primero
        return avisos.OrderBy(a => a.Prioridad).ToList();
    }

    /// <summary>Primeros 3 elementos y "y N más".</summary>
    private static string Resumir(List<string> elementos) =>
        string.Join(", ", elementos.Take(3)) + (elementos.Count > 3 ? $" y {elementos.Count - 3} más" : "");
}

public class NotificacionDto
{
    public const string Urgente = "Urgente";
    public const string Atencion = "Atencion";
    public const string Informacion = "Informacion";

    public NotificacionDto(string nivel, string titulo, string detalle, string destino, string accion)
    {
        Nivel = nivel;
        Titulo = titulo;
        Detalle = detalle;
        Destino = destino;
        Accion = accion;
    }

    public string Nivel { get; }
    public string Titulo { get; }
    public string Detalle { get; }

    /// <summary>Pantalla a la que lleva el aviso (NotificacionService.DestinoStock / DestinoRecetas).</summary>
    public string Destino { get; }
    public string Accion { get; }

    public int Prioridad => Nivel switch { Urgente => 0, Atencion => 1, _ => 2 };

    // Colores del aviso (se enlazan directo desde el XAML)
    public string ColorAcento => Nivel switch { Urgente => "#D64550", Atencion => "#E0A93B", _ => "#1F7A6C" };
    public string ColorFondo => Nivel switch { Urgente => "#FBE6E8", Atencion => "#FDF3E1", _ => "#E3F4EE" };
}
