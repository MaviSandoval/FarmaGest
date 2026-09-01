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
}