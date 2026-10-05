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

        SubtituloText.Text = "Existencias actuales. El stock lo actualiza el Farmacéutico desde «Productos». " +
                             $"Se considera stock bajo por debajo de {EstadosStock.UmbralStockBajo} unidades.";

        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
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

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarText.Text.Trim();

        StockGrid.ItemsSource = _productos
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
