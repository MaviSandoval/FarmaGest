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

    /// <summary>Busca afiliados por N° de afiliado, nombre o apellido.</summary>
    public async Task<List<AfiliadoDto>> BuscarAfiliadosAsync(string texto)
    {
        texto = texto.Trim();
        if (texto.Length == 0)
            return new List<AfiliadoDto>();

        await using var context = await _factory.CreateDbContextAsync();

        int.TryParse(texto, out int numero);

        return await context.Afiliados
            .AsNoTracking()
            .Where(a => a.Id == numero ||
                        a.Apellido.Contains(texto) ||
                        a.Nombre.Contains(texto))
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Take(50)
            .Select(a => new AfiliadoDto
            {
                Nombre = a.Nombre + " " + a.Apellido,
                NumeroAfiliado = a.Id,
                ObraSocial = a.Plan.ObraSocial.Nombre,
                Plan = a.Plan.NombrePlan,
                Activo = a.Activo
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

    public string Estado => Activo ? "Activo" : "Inactivo";
}
