using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// Control de stock (Farmacéutico): consulta de existencias y alertas.
/// El stock se modifica desde Productos (Editar) y baja solo con las ventas.
/// </summary>
public partial class StockFarmaceuticoPage : Page
{
    private readonly StockService _stockService;
    private List<StockProductoDto> _productos = new();

    public StockFarmaceuticoPage(StockService stockService)
    {
        InitializeComponent();
        _stockService = stockService;

        SubtituloText.Text = "Revisá qué productos están con stock bajo o sin stock. Para cambiar el stock, editá el producto en «Productos». " +
                             $"Se considera stock bajo por debajo de {EstadosStock.UmbralStockBajo} unidades.";

        Loaded += async (_, _) => await CargarAsync();
    }

    private StockProductoDto? ProductoSeleccionado => StockGrid.SelectedItem as StockProductoDto;

    private async Task CargarAsync(int? seleccionarId = null)
    {
        try
        {
            _productos = await _stockService.ObtenerStockAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar el stock.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ActivosText.Text = _productos.Count.ToString();
        NormalText.Text = _productos.Count(p => p.Estado == EstadosStock.Normal).ToString();
        BajoText.Text = _productos.Count(p => p.Estado == EstadosStock.Bajo).ToString();
        SinStockText.Text = _productos.Count(p => p.Estado == EstadosStock.SinStock).ToString();

        AplicarFiltro(seleccionarId);
    }

    private void AplicarFiltro(int? seleccionarId = null)
    {
        string texto = BuscarText.Text.Trim();

        var lista = _productos
            .Where(p => texto.Length == 0 || p.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Where(p => EstadoCombo.SelectedIndex switch
            {
                1 => p.Estado == EstadosStock.Normal,
                2 => p.Estado == EstadosStock.Bajo,
                3 => p.Estado == EstadosStock.SinStock,
                _ => true
            })
            .ToList();

        StockGrid.ItemsSource = lista;
        StockGrid.SelectedItem = lista.FirstOrDefault(p => p.Id == seleccionarId) ?? lista.FirstOrDefault();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro(ProductoSeleccionado?.Id);
    }
}
