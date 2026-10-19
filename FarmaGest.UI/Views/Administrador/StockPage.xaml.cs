using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>Stock en modo consulta para el Administrador (el control lo hace el Farmacéutico).</summary>
public partial class StockPage : Page
{
    private readonly StockService _stockService;
    private List<StockProductoDto> _productos = new();

    public StockPage(StockService stockService)
    {
        InitializeComponent();
        _stockService = stockService;

        SubtituloText.Text = "Productos, precios y existencias (solo consulta). El alta, la edición y el stock los maneja el Farmacéutico. " +
                             $"Se considera stock bajo por debajo de {EstadosStock.UmbralStockBajo} unidades.";

        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            _productos = await _stockService.ObtenerStockAsync(incluirBajas: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar el stock.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Los indicadores cuentan solo los productos activos
        var activos = _productos.Where(p => p.Activo).ToList();
        ActivosText.Text = activos.Count.ToString();
        NormalText.Text = activos.Count(p => p.Estado == EstadosStock.Normal).ToString();
        BajoText.Text = activos.Count(p => p.Estado == EstadosStock.Bajo).ToString();
        SinStockText.Text = activos.Count(p => p.Estado == EstadosStock.SinStock).ToString();

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarText.Text.Trim();

        StockGrid.ItemsSource = _productos
            .Where(p => VerBajasCheck.IsChecked == true || p.Activo)
            .Where(p => texto.Length == 0 || p.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Where(p => EstadoCombo.SelectedIndex switch
            {
                1 => p.Estado == EstadosStock.Normal,
                2 => p.Estado == EstadosStock.Bajo,
                3 => p.Estado == EstadosStock.SinStock,
                _ => true
            })
            .ToList();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro();
    }
}
