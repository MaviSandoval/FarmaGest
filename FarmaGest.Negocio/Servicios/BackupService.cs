using FarmaGest.Datos.Repositorios;

namespace FarmaGest.Negocio.Servicios;

/// <summary>Copias de seguridad de la base (solo Administrador).</summary>
public class BackupService
{
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

    public Task RestaurarBackupAsync(string archivo)
    {
        if (string.IsNullOrWhiteSpace(archivo) || !archivo.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Elegí un archivo de copia de seguridad (.bak).");

        return _repositorio.RestaurarBackupAsync(archivo);
    }
}
