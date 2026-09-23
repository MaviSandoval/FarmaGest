using System.Data;
using FarmaGest.Datos.Contexto;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Datos.Repositorios;

/// <summary>
/// Resultado que devuelven los procedimientos almacenados:
/// código de error (0 = OK), mensaje y, en el alta, el id generado.
/// </summary>
public record ResultadoProcedimiento(int CodigoError, string MensajeError, int IdGenerado = 0)
{
    public bool Exito => CodigoError == 0;
}

/// <summary>
/// Acceso a datos de Usuario mediante procedimientos almacenados.
/// Alta  -> sp_Usuario_Alta
/// Modificación, baja lógica, cambio de contraseña y foto -> sp_Usuario_Modificacion
/// </summary>
public class UsuarioRepositorio
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public UsuarioRepositorio(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================================
    // ALTA
    // =========================================================
    public async Task<ResultadoProcedimiento> AltaAsync(
        string dni,
        string nombre,
        string apellido,
        string? email,
        string contrasenaHash,
        int idRol)
    {
        var idUsuario    = ParametroSalida("@id_usuario", SqlDbType.Int);
        var codigoError  = ParametroSalida("@codigo_error", SqlDbType.Int);
        var mensajeError = ParametroSalida("@mensaje_error", SqlDbType.VarChar, 500);

        await using var context = await _factory.CreateDbContextAsync();

        await context.Database.ExecuteSqlRawAsync(
            "EXEC sp_Usuario_Alta " +
            "@dni = @dni, @nombre = @nombre, @apellido = @apellido, @email = @email, " +
            "@contrasena = @contrasena, @id_rol = @id_rol, " +
            "@id_usuario = @id_usuario OUTPUT, " +
            "@codigo_error = @codigo_error OUTPUT, " +
            "@mensaje_error = @mensaje_error OUTPUT",
            Parametro("@dni", SqlDbType.VarChar, dni, 15),
            Parametro("@nombre", SqlDbType.VarChar, nombre, 80),
            Parametro("@apellido", SqlDbType.VarChar, apellido, 80),
            Parametro("@email", SqlDbType.VarChar, email, 120),
            Parametro("@contrasena", SqlDbType.VarChar, contrasenaHash, 256),
            Parametro("@id_rol", SqlDbType.Int, idRol),
            idUsuario, codigoError, mensajeError);

        return new ResultadoProcedimiento(
            LeerEntero(codigoError),
            LeerTexto(mensajeError),
            LeerEntero(idUsuario));
    }

    // =========================================================
    // MODIFICACIÓN (también baja lógica, contraseña y foto)
    // Los parámetros en null no se modifican.
    // =========================================================
    public async Task<ResultadoProcedimiento> ModificacionAsync(
        int idUsuario,
        string? dni = null,
        string? nombre = null,
        string? apellido = null,
        string? email = null,
        int? idRol = null,
        bool? estado = null,
        string? contrasenaHash = null,
        bool? requiereCambioContrasena = null,
        byte[]? foto = null,
        bool quitarFoto = false)
    {
        var codigoError  = ParametroSalida("@codigo_error", SqlDbType.Int);
        var mensajeError = ParametroSalida("@mensaje_error", SqlDbType.VarChar, 500);

        await using var context = await _factory.CreateDbContextAsync();

        await context.Database.ExecuteSqlRawAsync(
            "EXEC sp_Usuario_Modificacion " +
            "@id_usuario = @id_usuario, @dni = @dni, @nombre = @nombre, @apellido = @apellido, " +
            "@email = @email, @id_rol = @id_rol, @estado = @estado, @contrasena = @contrasena, " +
            "@requiere_cambio_contrasena = @requiere_cambio_contrasena, " +
            "@foto = @foto, @quitar_foto = @quitar_foto, " +
            "@codigo_error = @codigo_error OUTPUT, " +
            "@mensaje_error = @mensaje_error OUTPUT",
            Parametro("@id_usuario", SqlDbType.Int, idUsuario),
            Parametro("@dni", SqlDbType.VarChar, dni, 15),
            Parametro("@nombre", SqlDbType.VarChar, nombre, 80),
            Parametro("@apellido", SqlDbType.VarChar, apellido, 80),
            Parametro("@email", SqlDbType.VarChar, email, 120),
            Parametro("@id_rol", SqlDbType.Int, idRol),
            Parametro("@estado", SqlDbType.Bit, estado),
            Parametro("@contrasena", SqlDbType.VarChar, contrasenaHash, 256),
            Parametro("@requiere_cambio_contrasena", SqlDbType.Bit, requiereCambioContrasena),
            Parametro("@foto", SqlDbType.VarBinary, foto, -1),
            Parametro("@quitar_foto", SqlDbType.Bit, quitarFoto),
            codigoError, mensajeError);

        return new ResultadoProcedimiento(
            LeerEntero(codigoError),
            LeerTexto(mensajeError));
    }

    // =========================================================
    // AUXILIARES
    // =========================================================
    private static SqlParameter Parametro(string nombre, SqlDbType tipo, object? valor, int tamano = 0)
    {
        var parametro = new SqlParameter(nombre, tipo)
        {
            Value = valor ?? DBNull.Value
        };

        if (tamano != 0)
            parametro.Size = tamano;

        return parametro;
    }

    private static SqlParameter ParametroSalida(string nombre, SqlDbType tipo, int tamano = 0)
    {
        var parametro = new SqlParameter(nombre, tipo)
        {
            Direction = ParameterDirection.Output
        };

        if (tamano != 0)
            parametro.Size = tamano;

        return parametro;
    }

    private static int LeerEntero(SqlParameter parametro) =>
        parametro.Value is int valor ? valor : 0;

    private static string LeerTexto(SqlParameter parametro) =>
        parametro.Value as string ?? string.Empty;
}
