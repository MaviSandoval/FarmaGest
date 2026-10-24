using FarmaGest.Datos.Repositorios;

namespace FarmaGest.Negocio.Servicios;

/// <summary>Copias de seguridad de la base (solo Administrador).</summary>
public class BackupService
{
    /// <summary>Descripción con la que se registran las copias automáticas (se ve en el historial).</summary>
    public const string GeneradoPorAutomatico = "Automático";

    /// <summary>Cantidad de copias automáticas que se conservan en disco.</summary>
    private const int AutomaticosAConservar = 7;

    private readonly BackupRepositorio _repositorio;

    public BackupService(BackupRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<string?> ObtenerCarpetaPorDefectoAsync() => _repositorio.ObtenerCarpetaPorDefectoAsync();

    public Task<List<BackupInfo>> ObtenerHistorialAsync() => _repositorio.ObtenerHistorialAsync();

    public Task<string> GenerarBackupAsync(string carpeta, string generadoPor)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            throw new InvalidOperationException("Elegí la carpeta donde guardar la copia.");

        return _repositorio.GenerarBackupAsync(carpeta.Trim(), generadoPor);
    }

    /// <summary>
    /// Copia automática diaria: la genera solo si todavía no hay una automática de hoy.
    /// Se guarda en la carpeta por defecto de SQL Server. Devuelve true si generó una copia.
    /// </summary>
    public async Task<bool> GenerarAutomaticoSiFaltaAsync()
    {
        var ultima = await _repositorio.ObtenerHistorialAsync(1, GeneradoPorAutomatico);
        if (ultima.Count > 0 && ultima[0].Fecha.Date >= DateTime.Today)
            return false;

        string? carpeta = await _repositorio.ObtenerCarpetaPorDefectoAsync();
        if (string.IsNullOrWhiteSpace(carpeta))
            throw new InvalidOperationException("SQL Server no tiene configurada una carpeta de copias por defecto.");

        await _repositorio.GenerarBackupAsync(carpeta, GeneradoPorAutomatico);
        BorrarAutomaticosViejos(await _repositorio.ObtenerHistorialAsync(50, GeneradoPorAutomatico));
        return true;
    }

    /// <summary>Deja solo las últimas copias automáticas. Las manuales nunca se borran.</summary>
    private static void BorrarAutomaticosViejos(List<BackupInfo> automaticos)
    {
        foreach (var copia in automaticos.Skip(AutomaticosAConservar))
        {
            try
            {
                if (File.Exists(copia.Archivo))
                    File.Delete(copia.Archivo);
            }
            catch
            {
                // Sin permiso sobre la carpeta de SQL Server: se conserva el archivo, no es un error grave
            }
        }
    }

    public Task RestaurarBackupAsync(string archivo)
    {
        if (string.IsNullOrWhiteSpace(archivo) || !archivo.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Elegí un archivo de copia de seguridad (.bak).");

        return _repositorio.RestaurarBackupAsync(archivo);
    }
}
