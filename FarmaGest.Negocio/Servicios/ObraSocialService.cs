using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>Consulta de obras sociales, planes, coberturas y afiliados (solo lectura).</summary>
public class ObraSocialService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public ObraSocialService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ObraSocialDto>> ObtenerObrasSocialesAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var obras = await context.ObrasSociales
            .AsNoTracking()
            .Where(o => o.Estado)
            .Include(o => o.Planes).ThenInclude(p => p.Coberturas).ThenInclude(c => c.Producto)
            .AsSplitQuery()
            .OrderBy(o => o.Nombre)
            .ToListAsync();

        return obras.Select(o => new ObraSocialDto
        {
            Nombre = o.Nombre,
            Codigo = o.Codigo,
            Telefono = o.Telefono ?? "—",
            Planes = o.Planes
                .OrderBy(p => p.NombrePlan)
                .Select(p => new PlanDto
                {
                    Nombre = p.NombrePlan,
                    Descripcion = p.Descripcion ?? "",
                    Coberturas = p.Coberturas
                        .OrderBy(c => c.Producto.Descripcion)
                        .Select(c => new CoberturaPlanDto { Producto = c.Producto.Descripcion, Porcentaje = c.PorcentajeCobertura })
                        .ToList()
                })
                .ToList()
        }).ToList();
    }

    /// <summary>
    /// Busca afiliados por N° de afiliado, o por palabras que aparezcan en el nombre,
    /// el apellido o la obra social ("juan perez", "perez pami"...).
    /// No distingue mayúsculas ni tildes: "Perez" encuentra "Pérez".
    /// </summary>
    public async Task<List<AfiliadoDto>> BuscarAfiliadosAsync(string texto)
    {
        texto = texto.Trim();
        if (texto.Length == 0)
            return new List<AfiliadoDto>();

        await using var context = await _factory.CreateDbContextAsync();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var consulta = context.Afiliados.AsNoTracking();

        if (texto.All(char.IsDigit) && int.TryParse(texto, out int numero))
        {
            consulta = consulta.Where(a => a.Id == numero);
        }
        else
        {
            // Cada palabra tiene que aparecer en nombre, apellido u obra social (sin importar tildes)
            foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                consulta = consulta.Where(a =>
                    EF.Functions.Collate(a.Nombre + " " + a.Apellido + " " + a.Plan.ObraSocial.Nombre,
                        "Latin1_General_CI_AI").Contains(palabra));
            }
        }

        return await consulta
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Take(50)
            .Select(a => new AfiliadoDto
            {
                Nombre = a.Nombre + " " + a.Apellido,
                NumeroAfiliado = a.Id,
                ObraSocial = a.Plan.ObraSocial.Nombre,
                Plan = a.Plan.NombrePlan,
                Activo = a.Activo,
                // Recetas que todavía se pueden usar: no rechazadas, no vencidas y sin venta activa
                RecetasVigentes = a.Recetas.Count(r =>
                    r.Estado != false &&
                    r.FechaVencimiento >= hoy &&
                    !context.Ventas.Any(v => v.RecetaId == r.Id && v.Estado)),
                UltimaReceta = a.Recetas.Max(r => (DateOnly?)r.FechaEmision)
            })
            .ToListAsync();
    }
}

public class ObraSocialDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public List<PlanDto> Planes { get; set; } = new();
}

public class PlanDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public List<CoberturaPlanDto> Coberturas { get; set; } = new();
}

public class CoberturaPlanDto
{
    public string Producto { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
}

public class AfiliadoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int NumeroAfiliado { get; set; }
    public string ObraSocial { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int RecetasVigentes { get; set; }
    public DateOnly? UltimaReceta { get; set; }

    public string Estado => Activo ? "Activo" : "Inactivo";
    public string UltimaRecetaTexto => UltimaReceta?.ToString("dd/MM/yyyy") ?? "—";
}
