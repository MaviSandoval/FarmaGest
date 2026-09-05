using System.Threading.Tasks;
using BCrypt.Net;
using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class UsuarioService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public UsuarioService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

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

    /// <summary>Cambia la contraseña del usuario y desactiva el flag de cambio obligatorio.</summary>
    public async Task<bool> CambiarContrasenaAsync(int idUsuario, string nuevaContrasena)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario);
        if (usuario == null)
            return false;

        usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);
        usuario.RequiereCambioContrasena = false;

        await context.SaveChangesAsync();
        return true;
    }
}