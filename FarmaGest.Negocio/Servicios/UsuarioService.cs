using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FarmaGest.Datos.Contexto;
using FarmaGest.Datos.Repositorios;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class UsuarioService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;
    private readonly UsuarioRepositorio _usuarioRepositorio;

    public UsuarioService(
        IDbContextFactory<FarmaGestDbContext> factory,
        UsuarioRepositorio usuarioRepositorio)
    {
        _factory = factory;
        _usuarioRepositorio = usuarioRepositorio;
    }

    // =========================================================
    // LOGIN (solo lectura)
    // =========================================================
    public async Task<Usuario?> ValidarCredencialesAsync(string emailODni, string contrasena)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var usuario = await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u =>
                (u.Email == emailODni || u.Dni == emailODni) && u.Estado);

        if (usuario == null)
            return null;

        bool contrasenaValida = BCrypt.Net.BCrypt.Verify(contrasena, usuario.Contrasena);

        return contrasenaValida ? usuario : null;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == idUsuario);
    }

    // =========================================================
    // CAMBIO DE CONTRASEÑA OBLIGATORIO (primer ingreso)
    // =========================================================
    /// <summary>Cambia la contraseña del usuario y desactiva el flag de cambio obligatorio.</summary>
    public async Task<bool> CambiarContrasenaAsync(int idUsuario, string nuevaContrasena)
    {
        var resultado = await _usuarioRepositorio.ModificacionAsync(
            idUsuario,
            contrasenaHash: BCrypt.Net.BCrypt.HashPassword(nuevaContrasena),
            requiereCambioContrasena: false);

        return resultado.Exito;
    }

    // =========================================================
    // MI PERFIL
    // =========================================================

    /// <summary>Actualiza nombre, apellido y email del propio usuario.</summary>
    public async Task ActualizarDatosPerfilAsync(int idUsuario, string nombre, string apellido, string? email)
    {
        nombre = nombre.Trim();
        apellido = apellido.Trim();
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            throw new InvalidOperationException("El nombre y el apellido son obligatorios.");

        if (nombre.Any(char.IsDigit) || apellido.Any(char.IsDigit))
            throw new InvalidOperationException("El nombre y el apellido no pueden contener números.");

        if (email != null && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new InvalidOperationException("El correo electrónico ingresado no es válido.");

        var resultado = await _usuarioRepositorio.ModificacionAsync(
            idUsuario,
            nombre: nombre,
            apellido: apellido,
            email: email);

        if (!resultado.Exito)
            throw new InvalidOperationException(resultado.MensajeError);
    }

    /// <summary>Cambia la contraseña verificando primero la actual.</summary>
    public async Task CambiarContrasenaPerfilAsync(int idUsuario, string contrasenaActual, string nuevaContrasena)
    {
        var usuario = await ObtenerPorIdAsync(idUsuario)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (!BCrypt.Net.BCrypt.Verify(contrasenaActual, usuario.Contrasena))
            throw new InvalidOperationException("La contraseña actual no es correcta.");

        if (nuevaContrasena.Length < 6)
            throw new InvalidOperationException("La nueva contraseña debe tener al menos 6 caracteres.");

        if (!await CambiarContrasenaAsync(idUsuario, nuevaContrasena))
            throw new InvalidOperationException("No se pudo actualizar la contraseña.");
    }

    /// <summary>Guarda la foto de perfil. Si foto es null, la elimina.</summary>
    public async Task ActualizarFotoAsync(int idUsuario, byte[]? foto)
    {
        var resultado = foto == null
            ? await _usuarioRepositorio.ModificacionAsync(idUsuario, quitarFoto: true)
            : await _usuarioRepositorio.ModificacionAsync(idUsuario, foto: foto);

        if (!resultado.Exito)
            throw new InvalidOperationException(resultado.MensajeError);
    }
}
