using FarmaGest.Dominio;

namespace FarmaGest.Negocio.Servicios;

public class SesionUsuarioService
{
    public Usuario? UsuarioActual { get; private set; }

    public bool HayUsuarioAutenticado => UsuarioActual != null;

    public void IniciarSesion(Usuario usuario)
    {
        UsuarioActual = usuario;
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
    }
}