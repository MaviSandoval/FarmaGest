using System.Data;
using FarmaGest.Datos.Conexion;
using Microsoft.Data.SqlClient;

namespace FarmaGest.Datos.Repositorios;

/// <summary>Copia de seguridad registrada por SQL Server (tabla msdb.dbo.backupset).</summary>
public record BackupInfo(DateTime Fecha, string Archivo, decimal TamanoBytes, string GeneradaPor)
{
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public string Tamano => TamanoBytes >= 1024 * 1024
        ? $"{TamanoBytes / 1024 / 1024:N1} MB"
        : $"{TamanoBytes / 1024:N0} KB";
}

/// <summary>
/// Copias de seguridad de FarmaGestDB con BACKUP / RESTORE de SQL Server.
/// Importante: el archivo .bak lo escribe el servicio de SQL Server (no la app),
/// así que la carpeta tiene que ser accesible para ese servicio.
/// </summary>
public class BackupRepositorio
{
    private const string NombreBase = "FarmaGestDB";

    /// <summary>Carpeta de backups configurada en la instancia de SQL Server.</summary>
    public async Task<string?> ObtenerCarpetaPorDefectoAsync()
    {
        await using var conexion = ConexionSingleton.Instancia.CrearConexion();
        await conexion.OpenAsync();

        await using var comando = new SqlCommand(
            "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000));", conexion);

        var resultado = await comando.ExecuteScalarAsync();
        return resultado as string;
    }

    /// <summary>Genera un .bak completo de la base y devuelve la ruta del archivo.</summary>
    public async Task<string> GenerarBackupAsync(string carpeta, string generadoPor)
    {
        string archivo = Path.Combine(carpeta, $"{NombreBase}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bak");

        await using var conexion = ConexionSingleton.Instancia.CrearConexion();
        await conexion.OpenAsync();

        // BACKUP acepta variables: así la ruta va como parámetro y no concatenada
        await using var comando = new SqlCommand(
            $"BACKUP DATABASE [{NombreBase}] TO DISK = @ruta WITH INIT, NAME = @nombre, DESCRIPTION = @descripcion;",
            conexion)
        {
            CommandTimeout = 600
        };
        comando.Parameters.Add("@ruta", SqlDbType.NVarChar, 260).Value = archivo;
        comando.Parameters.Add("@nombre", SqlDbType.NVarChar, 128).Value = $"{NombreBase} - copia completa";
        comando.Parameters.Add("@descripcion", SqlDbType.NVarChar, 255).Value = generadoPor;

        await comando.ExecuteNonQueryAsync();
        return archivo;
    }

    /// <summary>Últimas copias completas registradas por SQL Server.</summary>
    public async Task<List<BackupInfo>> ObtenerHistorialAsync(int maximo = 20)
    {
        await using var conexion = ConexionSingleton.Instancia.CrearConexion();
        await conexion.OpenAsync();

        await using var comando = new SqlCommand(@"
            SELECT TOP (@maximo)
                   b.backup_finish_date,
                   m.physical_device_name,
                   b.backup_size,
                   ISNULL(b.description, '')
            FROM msdb.dbo.backupset b
            JOIN msdb.dbo.backupmediafamily m ON m.media_set_id = b.media_set_id
            WHERE b.database_name = @base AND b.type = 'D'
            ORDER BY b.backup_finish_date DESC;", conexion);
        comando.Parameters.Add("@maximo", SqlDbType.Int).Value = maximo;
        comando.Parameters.Add("@base", SqlDbType.NVarChar, 128).Value = NombreBase;

        var lista = new List<BackupInfo>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            lista.Add(new BackupInfo(
                lector.IsDBNull(0) ? DateTime.MinValue : lector.GetDateTime(0),
                lector.IsDBNull(1) ? "" : lector.GetString(1),
                lector.IsDBNull(2) ? 0 : Convert.ToDecimal(lector.GetValue(2)),
                lector.GetString(3)));
        }

        return lista;
    }

    /// <summary>
    /// Restaura la base desde un .bak. Se conecta a master, deja la base en
    /// modo de un solo usuario (corta las otras conexiones), restaura y la
    /// vuelve a modo multiusuario.
    /// </summary>
    public async Task RestaurarBackupAsync(string archivo)
    {
        // Se cierran las conexiones que la app tiene guardadas en el pool
        SqlConnection.ClearAllPools();

        var builder = new SqlConnectionStringBuilder(ConexionSingleton.Instancia.CadenaConexion)
        {
            InitialCatalog = "master",
            Pooling = false
        };

        await using var conexion = new SqlConnection(builder.ConnectionString);
        await conexion.OpenAsync();

        await using var comando = new SqlCommand($@"
            ALTER DATABASE [{NombreBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            BEGIN TRY
                RESTORE DATABASE [{NombreBase}] FROM DISK = @ruta WITH REPLACE;
            END TRY
            BEGIN CATCH
                ALTER DATABASE [{NombreBase}] SET MULTI_USER;
                THROW;
            END CATCH;
            ALTER DATABASE [{NombreBase}] SET MULTI_USER;", conexion)
        {
            CommandTimeout = 600
        };
        comando.Parameters.Add("@ruta", SqlDbType.NVarChar, 260).Value = archivo;

        await comando.ExecuteNonQueryAsync();
    }
}
