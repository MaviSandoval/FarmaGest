using System;
using System.Collections.Generic;
using System.Linq;
using FarmaGest.Dominio;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Implementación TEMPORAL en memoria para poder probar el login sin tener
/// todavía conectada la base de datos. Cuando FarmaGest.Datos tenga el
/// repositorio real de usuarios (con hash de contraseña, etc.), esta clase
/// se reemplaza por esa implementación real.
/// </summary>
public class UsuarioService
{
    private static readonly List<(string Email, string Contrasena, string Perfil)> UsuariosDePrueba = new()
    {
        ("admin@farmacia.es", "admin123", "Administrador"),
        ("farmaceutico@farmacia.es", "farma123", "Farmaceutico"),
        ("cajero@farmacia.es", "cajero123", "Cajero"),
    };

    public Usuario? ValidarCredenciales(string usuario, string contrasena)
    {
        var encontrado = UsuariosDePrueba.FirstOrDefault(u =>
            u.Email.Equals(usuario, StringComparison.OrdinalIgnoreCase) &&
            u.Contrasena == contrasena);

        if (encontrado.Email is null)
            return null;

        return new Usuario
        {
            Email = encontrado.Email,
            Contrasena = encontrado.Contrasena,
            Nombre = encontrado.Perfil,
            Apellido = string.Empty,
            Dni = string.Empty,
            Rol = new Rol { Nombre = encontrado.Perfil }
        };
    }
}