using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// Validación de recetas. Las recetas llegan desde las obras sociales
/// (cargadas en la base de datos): el Farmacéutico las valida o las rechaza.
/// </summary>
public partial class ValidacionRecetasPage : Page
{
    private readonly RecetaService _recetaService;
    private List<RecetaDto> _recetas = new();

    public ValidacionRecetasPage(RecetaService recetaService)
    {
        InitializeComponent();
        _recetaService = recetaService;
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync(int? seleccionarNumero = null)
    {
        try
        {
            _recetas = await _recetaService.ObtenerRecetasAsync();
        }
        catch (Exception ex)
        {
            Error("No se pudieron cargar las recetas", ex);
            return;
        }

        AplicarFiltro(seleccionarNumero);
    }

    private void AplicarFiltro(int? seleccionarNumero = null)
    {
        string texto = BuscarText.Text.Trim();

        string? estado = EstadoCombo.SelectedIndex switch
        {
            1 => Receta.Pendiente,
            2 => Receta.Validada,
            3 => Receta.Rechazada,
            _ => null
        };

        var lista = _recetas
            .Where(r => estado == null || r.Estado == estado)
            .Where(r => texto.Length == 0 ||
                        r.Numero.ToString() == texto ||
                        r.Paciente.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        r.NumeroAfiliado.ToString() == texto ||
                        r.Medico.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        RecetasGrid.ItemsSource = lista;
        VacioText.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        RecetasGrid.SelectedItem =
            lista.FirstOrDefault(r => r.Numero == seleccionarNumero) ?? lista.FirstOrDefault();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        // Durante InitializeComponent los controles todavía no están listos
        if (!IsLoaded)
            return;

        AplicarFiltro((RecetasGrid.SelectedItem as RecetaDto)?.Numero);
    }

    private async void ActualizarRecetas_Click(object sender, RoutedEventArgs e)
    {
        await CargarAsync((RecetasGrid.SelectedItem as RecetaDto)?.Numero);
    }

    private async void Validar_Click(object sender, RoutedEventArgs e)
    {
        if (RecetasGrid.SelectedItem is not RecetaDto receta)
            return;

        try
        {
            await _recetaService.ValidarAsync(receta.Numero);
            await CargarAsync(receta.Numero);
            Aviso($"Receta N° {receta.Numero} validada. Ya se puede usar en una venta.");
        }
        catch (Exception ex)
        {
            Error("No se pudo validar la receta", ex);
        }
    }

    private async void Rechazar_Click(object sender, RoutedEventArgs e)
    {
        if (RecetasGrid.SelectedItem is not RecetaDto receta)
            return;

        var respuesta = MessageBox.Show($"¿Rechazar la receta N° {receta.Numero} de {receta.Paciente}?",
            "FarmaGest", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            await _recetaService.RechazarAsync(receta.Numero);
            await CargarAsync(receta.Numero);
            Aviso($"Receta N° {receta.Numero} rechazada.");
        }
        catch (Exception ex)
        {
            Error("No se pudo rechazar la receta", ex);
        }
    }

    private void IniciarVenta_Click(object sender, RoutedEventArgs e)
    {
        if (RecetasGrid.SelectedItem is not RecetaDto receta || !receta.PuedeIniciarVenta)
            return;

        var pagina = App.Services.GetRequiredService<NuevaVentaPage>();
        pagina.PreseleccionarReceta(receta.Numero);
        NavigationService?.Navigate(pagina);
    }

    private static void Aviso(string mensaje) =>
        MessageBox.Show(mensaje, "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);

    private static void Error(string titulo, Exception ex) =>
        MessageBox.Show($"{titulo}.\n\n{ex.Message}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Warning);
}
