using FarmaGest.Datos.Repositorios;

namespace FarmaGest.Negocio.Servicios;

/// <summary>Copias de seguridad de la base (solo Administrador).</summary>
public class BackupService
{
    /// <summary>Descripción con la que se registran las copias automáticas (se ve en el historial).</summary>
    public const string GeneradoPorAutomatico = "Automático";

    /// <summary>Parámetro con la carpeta elegida por el Administrador para las copias.</summary>
    private const string ParametroCarpeta = "CarpetaBackup";

    /// <summary>Parámetro que guarda si el backup automático está activo ("1" / "0").</summary>
    private const string ParametroAutomatico = "BackupAutomaticoActivo";

    /// <summary>Cantidad de copias automáticas que se conservan en disco.</summary>
    private const int AutomaticosAConservar = 7;

    private readonly BackupRepositorio _repositorio;

    public BackupService(BackupRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<string?> ObtenerCarpetaPorDefectoAsync() => _repositorio.ObtenerCarpetaPorDefectoAsync();

    /// <summary>Carpeta de copias: la que eligió el Administrador o, si nunca eligió, la de SQL Server.</summary>
    public async Task<string?> ObtenerCarpetaAsync() =>
        await _repositorio.ObtenerParametroAsync(ParametroCarpeta)
        ?? await _repositorio.ObtenerCarpetaPorDefectoAsync();

    /// <summary>Guarda la carpeta elegida: la usan las copias manuales y las automáticas.</summary>
    public Task GuardarCarpetaAsync(string carpeta) =>
        _repositorio.GuardarParametroAsync(ParametroCarpeta, carpeta.Trim());

    /// <summary>Si el .bak sigue estando en el disco (lo verifica SQL Server).</summary>
    public Task<bool> ArchivoExisteAsync(string ruta) => _repositorio.ArchivoExisteAsync(ruta);

    public Task<List<BackupInfo>> ObtenerHistorialAsync() => _repositorio.ObtenerHistorialAsync();

    /// <summary>Copias completas terminadas dentro del período ("hasta" inclusivo).</summary>
    public async Task<List<BackupInfo>> ObtenerHistorialAsync(DateTime desde, DateTime hasta)
    {
        var todas = await _repositorio.ObtenerHistorialAsync(1000);
        return todas
            .Where(b => b.Fecha >= desde.Date && b.Fecha < hasta.Date.AddDays(1))
            .ToList();
    }

    /// <summary>La copia completa más reciente (de cualquier fecha), o null si nunca se hizo una.</summary>
    public async Task<BackupInfo?> ObtenerUltimoAsync() =>
        (await _repositorio.ObtenerHistorialAsync(1)).FirstOrDefault();

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
    /// <summary>Si el backup automático está activo. Por defecto sí (si nunca se configuró).</summary>
    public async Task<bool> AutomaticoActivoAsync() =>
        await _repositorio.ObtenerParametroAsync(ParametroAutomatico) != "0";

    /// <summary>Activa o desactiva el backup automático diario (lo decide el Administrador).</summary>
    public Task CambiarAutomaticoAsync(bool activo) =>
        _repositorio.GuardarParametroAsync(ParametroAutomatico, activo ? "1" : "0");

    public async Task<bool> GenerarAutomaticoSiFaltaAsync()
    {
        // Desactivado por el Administrador: no se genera nada
        if (!await AutomaticoActivoAsync())
            return false;

        var ultima = await _repositorio.ObtenerHistorialAsync(1, GeneradoPorAutomatico);
        if (ultima.Count > 0 && ultima[0].Fecha.Date >= DateTime.Today)
            return false;

        // Misma carpeta que las copias manuales (así el Administrador las ve y se pueden limpiar las viejas)
        string? carpeta = await ObtenerCarpetaAsync();
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
