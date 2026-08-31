using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class GestionUsuariosService
{
    private readonly FarmaGestDbContext _context;

    public GestionUsuariosService(FarmaGestDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> ObtenerTodosAsync() =>
        await _context.Usuarios.Include(u => u.Rol).OrderBy(u => u.Apellido).ToListAsync();

    public async Task<List<Rol>> ObtenerRolesAsync() =>
        await _context.Roles.Where(r => r.Estado).OrderBy(r => r.Nombre).ToListAsync();

    /// <summary>Crea un usuario nuevo y devuelve la contraseña temporal en texto plano
    /// (única vez que existe en texto plano, para que el Admin la vea en pantalla).</summary>
    public async Task<string> CrearAsync(string dni, string nombre, string apellido, string? email, int rolId)
    {
        var contrasenaTemporal = GeneradorContrasena.Generar();

        var usuario = new Usuario
        {
            Dni = dni,
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            RolId = rolId,
            Estado = true,
            RequiereCambioContrasena = true,
            Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasenaTemporal)
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return contrasenaTemporal;
    }

    public async Task EditarAsync(int id, string dni, string nombre, string apellido, string? email, int rolId)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        usuario.Dni = dni;
        usuario.Nombre = nombre;
        usuario.Apellido = apellido;
        usuario.Email = email;
        usuario.RolId = rolId;

        await _context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        var usuario = await _context.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        usuario.Estado = activo;
        await _context.SaveChangesAsync();
    }
}