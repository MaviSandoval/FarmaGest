using System.Diagnostics;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Copia de seguridad automática diaria a las 00:00.
/// - Al abrir la app: si todavía no hay copia automática de hoy, la genera (recupera la
///   que se perdió si la app estaba cerrada a la medianoche).
/// - Mientras la app está abierta: espera hasta la próxima medianoche y vuelve a generar.
/// La copia manual del Administrador sigue funcionando aparte (ConfiguracionPage).
/// </summary>
public class BackupProgramadoService
{
    // Pequeño margen para no disparar unos milisegundos antes de las 00:00
    private static readonly TimeSpan Margen = TimeSpan.FromSeconds(5);

    private readonly BackupService _backupService;
    private int _iniciado;

    public DateTime? UltimaEjecucion { get; private set; }
    public string? UltimoError { get; private set; }

    public BackupProgramadoService(BackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>Arranca el programador en segundo plano (solo la primera vez que se llama).</summary>
    public void Iniciar()
    {
        if (Interlocked.Exchange(ref _iniciado, 1) == 1)
            return;

        _ = Task.Run(BucleAsync);
    }

    private async Task BucleAsync()
    {
        await EjecutarAsync();

        while (true)
        {
            var ahora = DateTime.Now;
            var proximaMedianoche = ahora.Date.AddDays(1);

            await Task.Delay(proximaMedianoche - ahora + Margen);
            await EjecutarAsync();
        }
    }

    private async Task EjecutarAsync()
    {
        try
        {
            if (await _backupService.GenerarAutomaticoSiFaltaAsync())
                UltimaEjecucion = DateTime.Now;

            UltimoError = null;
        }
        catch (Exception ex)
        {
            // No se corta la app: el error se muestra al Administrador en Configuración
            UltimoError = ex.Message;
            Debug.WriteLine($"[Backup automático] {ex}");
        }
    }
}
