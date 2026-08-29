namespace FarmaGest.Negocio.Servicios;

public class Usuario
{
    public string Perfil { get; set; } = string.Empty;
}

public class UsuarioService
{
    // Implementación mínima para permitir la compilación y pruebas
    public Usuario? ValidarCredenciales(string usuario, string contrasena)
    {
        // Credenciales de ejemplo: admin / 123
        if (usuario == "admin" && contrasena == "123")
        {
            return new Usuario { Perfil = "Administrador" };
        }

        return null;
    }
}
