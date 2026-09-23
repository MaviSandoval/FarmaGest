using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using FarmaGest.Datos.Contexto;
using FarmaGest.Datos.Repositorios;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class GestionUsuariosService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;
    private readonly UsuarioRepositorio _usuarioRepositorio;

    public GestionUsuariosService(
        IDbContextFactory<FarmaGestDbContext> factory,
        UsuarioRepositorio usuarioRepositorio)
    {
        _factory = factory;
        _usuarioRepositorio = usuarioRepositorio;
    }

    public async Task<List<Usuario>> ObtenerTodosAsync()
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        return await context.Usuarios
            .Include(u => u.Rol)
            .OrderBy(u => u.Apellido)
            .ToListAsync();
    }

    public async Task<List<Rol>> ObtenerRolesAsync()
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        return await context.Roles
            .Where(r => r.Estado)
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    // =========================================================
    // CREAR USUARIO
    // =========================================================
    public async Task<string> CrearAsync(
        string dni,
        string nombre,
        string apellido,
        string? email,
        int rolId)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        // Limpiar espacios
        dni = dni.Trim();
        nombre = nombre.Trim();
        apellido = apellido.Trim();
        email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();

        await ValidarUsuarioAsync(
            context,
            dni,
            nombre,
            apellido,
            email,
            rolId);

        var contrasenaTemporal =
            GeneradorContrasena.Generar();

        // Alta mediante el procedimiento almacenado sp_Usuario_Alta
        var resultado = await _usuarioRepositorio.AltaAsync(
            dni,
            nombre,
            apellido,
            email,
            BCrypt.Net.BCrypt.HashPassword(contrasenaTemporal),
            rolId);

        if (!resultado.Exito)
            throw new InvalidOperationException(resultado.MensajeError);

        return contrasenaTemporal;
    }

    // =========================================================
    // EDITAR USUARIO
    // =========================================================
    public async Task EditarAsync(
        int id,
        string dni,
        string nombre,
        string apellido,
        string? email,
        int rolId)
    {
        await using var context =
            await _factory.CreateDbContextAsync();

        // Limpiar espacios
        dni = dni.Trim();
        nombre = nombre.Trim();
        apellido = apellido.Trim();
        email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();

        await ValidarUsuarioAsync(
            context,
            dni,
            nombre,
            apellido,
            email,
            rolId,
            id);

        // Modificación mediante el procedimiento almacenado sp_Usuario_Modificacion
        var resultado = await _usuarioRepositorio.ModificacionAsync(
            id,
            dni: dni,
            nombre: nombre,
            apellido: apellido,
            email: email,
            idRol: rolId);

        if (!resultado.Exito)
            throw new InvalidOperationException(resultado.MensajeError);
    }

    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================
    // Baja lógica / reactivación: usa el mismo sp_Usuario_Modificacion
    public async Task CambiarEstadoAsync(
        int id,
        bool activo)
    {
        var resultado = await _usuarioRepositorio.ModificacionAsync(
            id,
            estado: activo);

        if (!resultado.Exito)
            throw new InvalidOperationException(resultado.MensajeError);
    }

    // =========================================================
    // VALIDACIONES
    // =========================================================
    private static async Task ValidarUsuarioAsync(
        FarmaGestDbContext context,
        string dni,
        string nombre,
        string apellido,
        string? email,
        int rolId,
        int? usuarioIdActual = null)
    {
        // ---------- DNI ----------

        if (string.IsNullOrWhiteSpace(dni))
            throw new InvalidOperationException(
                "El DNI es obligatorio.");

        if (!dni.All(char.IsDigit))
            throw new InvalidOperationException(
                "El DNI debe contener solamente números.");

        if (dni.Length < 7 || dni.Length > 8)
            throw new InvalidOperationException(
                "El DNI debe tener entre 7 y 8 dígitos.");

        var dniExistente = await context.Usuarios
            .AnyAsync(u =>
                u.Dni == dni &&
                (!usuarioIdActual.HasValue ||
                 u.Id != usuarioIdActual.Value));

        if (dniExistente)
            throw new InvalidOperationException(
                $"Ya existe un usuario registrado con el DNI {dni}.");

        // ---------- NOMBRE ----------

        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException(
                "El nombre es obligatorio.");

        if (nombre.Any(char.IsDigit))
            throw new InvalidOperationException(
                "El nombre no puede contener números.");

        // ---------- APELLIDO ----------

        if (string.IsNullOrWhiteSpace(apellido))
            throw new InvalidOperationException(
                "El apellido es obligatorio.");

        if (apellido.Any(char.IsDigit))
            throw new InvalidOperationException(
                "El apellido no puede contener números.");

        // ---------- EMAIL ----------

        if (!string.IsNullOrWhiteSpace(email))
        {
            // Debe tener formato básico: usuario@dominio.extension
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new InvalidOperationException(
                    "El correo electrónico ingresado no es válido.");
            }

            var emailExistente = await context.Usuarios
                .AnyAsync(u =>
                    u.Email != null &&
                    u.Email == email &&
                    (!usuarioIdActual.HasValue ||
                     u.Id != usuarioIdActual.Value));

            if (emailExistente)
            {
                throw new InvalidOperationException(
                    "Ya existe un usuario registrado con ese correo electrónico.");
            }
        }

        // ---------- ROL ----------

        if (rolId <= 0)
            throw new InvalidOperationException(
                "Debe seleccionar un rol.");

        var rolValido = await context.Roles
            .AnyAsync(r =>
                r.Id == rolId &&
                r.Estado);

        if (!rolValido)
            throw new InvalidOperationException(
                "El rol seleccionado no es válido.");
    }
}