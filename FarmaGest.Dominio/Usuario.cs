namespace FarmaGest.Dominio;

public class Usuario
{
    public int Id { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Contrasena { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
    public bool RequiereCambioContrasena { get; set; } = true;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
}