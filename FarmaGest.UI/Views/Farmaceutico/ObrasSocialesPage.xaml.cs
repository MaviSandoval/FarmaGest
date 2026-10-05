using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>Consulta de obras sociales, planes, coberturas y afiliados (solo lectura).</summary>
public partial class ObrasSocialesPage : Page
{
    private readonly ObraSocialService _obraSocialService;

    public ObrasSocialesPage(ObraSocialService obraSocialService)
    {
        InitializeComponent();
        _obraSocialService = obraSocialService;

        Loaded += async (_, _) => await CargarAsync();

        // Al cambiar de obra social, seleccionar su primer plan
        // (se hace en el Dispatcher para que primero se actualice la lista de planes)
        ObrasSocialesList.SelectionChanged += (_, _) => SeleccionarPrimerPlan();
    }

    private async Task CargarAsync()
    {
        try
        {
            ObrasSocialesList.ItemsSource = await _obraSocialService.ObtenerObrasSocialesAsync();
            ObrasSocialesList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar las obras sociales.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SeleccionarPrimerPlan()
    {
        Dispatcher.InvokeAsync(() => { PlanesList.SelectedIndex = 0; });
    }

    private async void BuscarAfiliado_Click(object sender, RoutedEventArgs e) => await BuscarAfiliadosAsync();

    private async void BuscarAfiliado_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await BuscarAfiliadosAsync();
    }

    private async Task BuscarAfiliadosAsync()
    {
        string texto = BuscarAfiliadoText.Text.Trim();
        // Un N° de afiliado puede tener un solo dígito; un nombre necesita al menos 2 letras
        if (texto.Length == 0 || (texto.Length < 2 && !texto.All(char.IsDigit)))
        {
            AfiliadosVacioText.Text = "Escribí al menos 2 caracteres para buscar.";
            AfiliadosVacioText.Visibility = Visibility.Visible;
            AfiliadosGrid.ItemsSource = null;
            AfiliadosGrid.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var afiliados = await _obraSocialService.BuscarAfiliadosAsync(texto);
            AfiliadosGrid.ItemsSource = afiliados;
            AfiliadosVacioText.Text = "No se encontraron afiliados con ese dato.";
            AfiliadosVacioText.Visibility = afiliados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AfiliadosGrid.Visibility = afiliados.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo buscar el afiliado.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
