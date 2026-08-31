using System;
using Microsoft.Data.SqlClient;

namespace FarmaGest.Datos.Conexion;

/// <summary>
/// Singleton que centraliza el acceso a la cadena de conexión y expone
/// un método para verificar la conectividad contra SQL Server.
/// No mantiene una SqlConnection abierta permanentemente: SqlConnection
/// ya usa connection pooling internamente, así que lo correcto es abrir
/// y cerrar una conexión nueva por operación usando esta cadena.
/// </summary>
public sealed class ConexionSingleton
{
    private static readonly Lazy<ConexionSingleton> _instancia =
        new(() => new ConexionSingleton());

    public static ConexionSingleton Instancia => _instancia.Value;

    public string CadenaConexion { get; }

    private ConexionSingleton()
    {
        CadenaConexion =
            "Server=MAVI\\SQLEXPRESS;" +
            "Database=FarmaGestDB;" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;";
    }

    public SqlConnection CrearConexion() => new(CadenaConexion);

    /// <summary>
    /// Abre y cierra una conexión de prueba. Devuelve true si SQL Server
    /// respondió correctamente.
    /// </summary>
    public bool ProbarConexion(out string mensaje)
    {
        try
        {
            using var conexion = CrearConexion();
            conexion.Open();
            mensaje = $"Conexión exitosa a '{conexion.Database}' en {conexion.DataSource}.";
            return true;
        }
        catch (Exception ex)
        {
            mensaje = $"Error al conectar: {ex.Message}";
            return false;
        }
    }
}