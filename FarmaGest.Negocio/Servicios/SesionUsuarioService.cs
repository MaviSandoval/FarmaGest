using FarmaGest.Dominio;

namespace FarmaGest.Negocio.Servicios;

public class SesionUsuarioService
{
    public Usuario? UsuarioActual { get; private set; }

    public bool HayUsuarioAutenticado => UsuarioActual != null;

    /// <summary>Se dispara cuando cambian los datos del usuario logueado (nombre, foto, etc.).</summary>
    public event Action? UsuarioActualizado;

    public void IniciarSesion(Usuario usuario)
    {
        UsuarioActual = usuario;
    }

    /// <summary>Reemplaza los datos del usuario en sesión (por ejemplo, después de editar Mi perfil).</summary>
    public void ActualizarUsuario(Usuario usuario)
    {
        UsuarioActual = usuario;
        UsuarioActualizado?.Invoke();
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
    }
}
