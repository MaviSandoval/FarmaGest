using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FarmaGest.UI.Servicios;

// Configuración por usuario de Windows. Nunca recibe objetos Usuario ni contraseñas.
public class UsuariosRecordadosService
{
    private readonly string _archivo;

    public UsuariosRecordadosService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FarmaGest", "usuarios-recordados.json")) { }

    internal UsuariosRecordadosService(string archivo) => _archivo = archivo;

    public IReadOnlyList<string> Leer()
    {
        try
        {
            if (!File.Exists(_archivo)) return Array.Empty<string>();
            var usuarios = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_archivo));
            return Normalizar(usuarios ?? new List<string>());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Una configuración ausente, dañada o inaccesible no impide autenticarse.
            return Array.Empty<string>();
        }
    }

    public bool Guardar(IEnumerable<string> usuarios)
    {
        var temporal = _archivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_archivo)!);
            File.WriteAllText(temporal, JsonSerializer.Serialize(Normalizar(usuarios)));
            File.Move(temporal, _archivo, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(temporal)) File.Delete(temporal); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    private static List<string> Normalizar(IEnumerable<string> usuarios)
        => usuarios.Where(u => !string.IsNullOrWhiteSpace(u) && u.Trim().Length <= 120)
            .Select(u => u.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
