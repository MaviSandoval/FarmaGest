using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>Catálogo de productos en modo consulta (el ABM lo hace el Farmacéutico).</summary>
public partial class ProductosPage : Page
{
    private readonly ProductoService _service;
    private List<ProductoCatalogoDto> _productos = new();

    public ProductosPage(ProductoService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            if (CategoriaFiltroCombo.ItemsSource == null)
            {
                var filtro = new List<Categoria> { new() { Id = 0, Nombre = "Todas las categorías" } };
                filtro.AddRange(await _service.ObtenerCategoriasAsync());
                CategoriaFiltroCombo.ItemsSource = filtro;
                CategoriaFiltroCombo.SelectedIndex = 0;
            }

            _productos = await _service.ObtenerCatalogoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar los productos.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var activos = _productos.Where(p => p.Activo).ToList();
        ActivosText.Text = activos.Count.ToString();
        ConRecetaText.Text = activos.Count(p => p.RequiereReceta).ToString();
        StockBajoText.Text = activos.Count(p => p.EstadoStock != EstadosStock.Normal).ToString();
        InactivosText.Text = _productos.Count(p => !p.Activo).ToString();

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarText.Text.Trim();
        int categoriaId = (CategoriaFiltroCombo.SelectedItem as Categoria)?.Id ?? 0;

        ProductosGrid.ItemsSource = _productos
            .Where(p => VerBajasCheck.IsChecked == true || p.Activo)
            .Where(p => categoriaId == 0 || p.CategoriaId == categoriaId)
            .Where(p => texto.Length == 0 || p.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro();
    }
}
