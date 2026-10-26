using FarmaGest.Datos.Contexto;
using FarmaGest.Datos.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Reportes del Administrador (los de ventas son del Farmacéutico):
///  1. Copias de seguridad ..... msdb.dbo.backupset + backupmediafamily (historial de SQL Server)
///  2. Usuarios ................ Usuario + Rol (gestión: estado, rol, cambio de contraseña)
///  3. Usuarios por rol ........ Usuario + Rol
/// </summary>
public class ReporteSistemaService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;
    private readonly BackupService _backupService;

    public ReporteSistemaService(IDbContextFactory<FarmaGestDbContext> factory, BackupService backupService)
    {
        _factory = factory;
        _backupService = backupService;
    }

    // =========================================================
    // 1. COPIAS DE SEGURIDAD
    // =========================================================
    public async Task<ReporteBackupsDto> BackupsAsync(DateTime desde, DateTime hasta)
    {
        var copias = await _backupService.ObtenerHistorialAsync(desde, hasta);
        var ultima = await _backupService.ObtenerUltimoAsync();

        // Días del período (hasta hoy) en los que no se hizo ninguna copia
        var fin = hasta.Date > DateTime.Today ? DateTime.Today : hasta.Date;
        var diasConCopia = copias.Select(c => c.Fecha.Date).ToHashSet();
        int diasSinCopia = 0;
        for (var d = desde.Date; d <= fin; d = d.AddDays(1))
            if (!diasConCopia.Contains(d))
                diasSinCopia++;

        var filas = new List<FilaBackupDto>();
        foreach (var c in copias)
        {
            bool automatica = c.GeneradaPor == BackupService.GeneradoPorAutomatico;
            filas.Add(new FilaBackupDto
            {
                Fecha = c.Fecha,
                Tipo = automatica ? "Automático" : "Manual",
                // A las 00:00, o al abrir FarmaGest si a esa hora estaba cerrado
                GeneradaPor = automatica ? "Sistema (automático)" : c.GeneradaPor,
                Archivo = c.Archivo,
                Tamano = c.Tamano,
                TamanoMb = Math.Round(c.TamanoBytes / 1024m / 1024m, 2),
                // Lo verifica SQL Server: la app puede no tener permiso sobre su carpeta
                Disponible = await _backupService.ArchivoExisteAsync(c.Archivo)
            });
        }

        return new ReporteBackupsDto
        {
            AutomaticoActivo = await _backupService.AutomaticoActivoAsync(),
            Ultima = ultima,
            DiasSinCopia = diasSinCopia,
            Copias = filas
        };
    }

    // =========================================================
    // 2. USUARIOS (gestión: sin datos de ventas, que son del Farmacéutico)
    // =========================================================
    public async Task<List<UsuarioReporteDto>> UsuariosAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Rol.Nombre).ThenBy(u => u.Apellido).ThenBy(u => u.Nombre)
            .Select(u => new UsuarioReporteDto
            {
                Usuario = u.Nombre + " " + u.Apellido,
                Dni = u.Dni,
                Email = u.Email ?? "—",
                Rol = u.Rol.Nombre,
                Activo = u.Estado,
                CambioPendiente = u.RequiereCambioContrasena
            })
            .ToListAsync();
    }

    // =========================================================
    // 3. USUARIOS POR ROL
    // =========================================================
    public async Task<List<UsuariosPorRolDto>> UsuariosPorRolAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new UsuariosPorRolDto
            {
                Rol = r.Nombre,
                Descripcion = r.Descripcion ?? "",
                Activos = r.Usuarios.Count(u => u.Estado),
                Inactivos = r.Usuarios.Count(u => !u.Estado),
                CambioPendiente = r.Usuarios.Count(u => u.Estado && u.RequiereCambioContrasena)
            })
            .ToListAsync();
    }
}

// =============================================================
// DTOs
// =============================================================

public class ReporteBackupsDto
{
    public bool AutomaticoActivo { get; set; }
    public BackupInfo? Ultima { get; set; }
    public int DiasSinCopia { get; set; }
    public List<FilaBackupDto> Copias { get; set; } = new();

    public int Automaticas => Copias.Count(c => c.Tipo == "Automático");
    public int Manuales => Copias.Count(c => c.Tipo == "Manual");
}

public class FilaBackupDto
{
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string GeneradaPor { get; set; } = string.Empty;
    public string Archivo { get; set; } = string.Empty;
    public string Tamano { get; set; } = string.Empty;
    public decimal TamanoMb { get; set; }
    public bool Disponible { get; set; }

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");

    // Se reutilizan los colores del badge: "Activo" verde, "Inactivo" rojo
    public string Estado => Disponible ? "Activo" : "Inactivo";
    public string EstadoTexto => Disponible ? "Disponible" : "No encontrado";
}

public class UsuarioReporteDto
{
    public string Usuario { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool CambioPendiente { get; set; }

    public string Estado => Activo ? "Activo" : "Inactivo";
    public string CambioPendienteTexto => CambioPendiente ? "Sí" : "No";
}

public class UsuariosPorRolDto
{
    public string Rol { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public int CambioPendiente { get; set; }

    public int Total => Activos + Inactivos;
}
