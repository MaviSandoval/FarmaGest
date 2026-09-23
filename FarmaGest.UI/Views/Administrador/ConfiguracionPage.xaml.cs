using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

// MAQUETA: configuración y copias de seguridad. Todavía no guarda ni ejecuta el BACKUP.
public partial class ConfiguracionPage : Page
{
    public ConfiguracionPage()
    {
        InitializeComponent();

        BackupsGrid.ItemsSource = new List<BackupFalso>
        {
            new() { Fecha = "22/09/2026 20:00", Archivo = "FarmaGestDB_2026-09-22.bak", Tamano = "12,4 MB", Usuario = "Admin Sistema" },
            new() { Fecha = "21/09/2026 20:00", Archivo = "FarmaGestDB_2026-09-21.bak", Tamano = "12,1 MB", Usuario = "Admin Sistema" },
            new() { Fecha = "20/09/2026 20:00", Archivo = "FarmaGestDB_2026-09-20.bak", Tamano = "11,9 MB", Usuario = "Admin Sistema" },
        };
    }

    private void Guardar_Click(object sender, RoutedEventArgs e) =>
        Aviso("Configuración guardada (maqueta).");

    private void ElegirCarpeta_Click(object sender, RoutedEventArgs e) =>
        Aviso("Acá se elegiría la carpeta donde guardar las copias (maqueta).");

    private void GenerarBackup_Click(object sender, RoutedEventArgs e) =>
        Aviso("Acá se ejecutaría BACKUP DATABASE FarmaGestDB en la carpeta elegida (maqueta).");

    private void RestaurarBackup_Click(object sender, RoutedEventArgs e) =>
        Aviso("Acá se elegiría un archivo .bak para restaurar la base (maqueta).");

    private static void Aviso(string mensaje) =>
        MessageBox.Show(mensaje, "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);

    private class BackupFalso
    {
        public string Fecha { get; set; } = "";
        public string Archivo { get; set; } = "";
        public string Tamano { get; set; } = "";
        public string Usuario { get; set; } = "";
    }
}
