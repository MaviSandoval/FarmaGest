namespace FarmaGest.Dominio;

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;

    public List<Usuario> Usuarios { get; set; } = new();
}