using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// Gestión de productos del Farmacéutico. Es el mismo formulario que antes tenía
/// el Administrador: Descripción, Categoría, Precio, Stock, Requiere receta y Es medicamento.
/// </summary>
public partial class ProductosFarmaceuticoPage : Page
{
    private readonly ProductoService _service;
    private List<Producto> _productos = new();
    private int? _idEnEdicion;

    public ProductosFarmaceuticoPage(ProductoService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            CategoriaCombo.ItemsSource = await _service.ObtenerCategoriasAsync();
            _productos = await _service.ObtenerTodosAsync();
        }
        catch (Exception ex)
        {
            MensajeText.Text = $"No se pudieron cargar los productos: {ex.Message}";
            return;
        }

        MostrarProductos();
    }

    private void MostrarProductos()
    {
        string texto = BuscarText.Text.Trim();

        ProductosGrid.ItemsSource = _productos
            .Where(p => texto.Length == 0 || p.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Select(p => new
            {
                Producto = p,
                p.Descripcion,
                p.Categoria,
                p.PrecioVenta,
                p.StockVenta,
                RequiereRecetaTexto = p.RequiereReceta ? "Sí" : "No",
                EstadoTexto = p.Estado ? "Activo" : "Inactivo",
                TextoAccionEstado = p.Estado ? "Dar de baja" : "Reactivar"
            })
            .ToList();
    }

    private void BuscarText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            MostrarProductos();
    }

    private async void GuardarButton_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        // Validación de tipos de dato: precio decimal (coma o punto) y stock entero
        var precioLeido = ValidacionEntrada.LeerDecimal(PrecioText.Text);
        var stockLeido = ValidacionEntrada.LeerEntero(StockText.Text);

        if (string.IsNullOrWhiteSpace(DescripcionText.Text) ||
            CategoriaCombo.SelectedItem is not Categoria categoria ||
            precioLeido is null ||
            stockLeido is null)
        {
            MensajeText.Text = "Completá Descripción, Categoría, Precio (número con hasta 2 decimales) y Stock (número entero).";
            return;
        }

        try
        {
            if (_idEnEdicion is null)
            {
                await _service.CrearAsync(
                    DescripcionText.Text, precioLeido.Value, stockLeido.Value,
                    RequiereRecetaCheck.IsChecked == true, EsMedicamentoCheck.IsChecked == true, categoria.Id);
            }
            else
            {
                await _service.EditarAsync(
                    _idEnEdicion.Value, DescripcionText.Text, precioLeido.Value, stockLeido.Value,
                    RequiereRecetaCheck.IsChecked == true, EsMedicamentoCheck.IsChecked == true, categoria.Id);
            }

            LimpiarFormulario();
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MensajeText.Text = $"Error al guardar: {ex.Message}";
        }
    }

    private void EditarProducto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not { } item) return;

        var producto = (Producto)item.GetType().GetProperty("Producto")!.GetValue(item)!;

        _idEnEdicion = producto.Id;
        TituloFormularioText.Text = $"Editando: {producto.Descripcion}";
        DescripcionText.Text = producto.Descripcion;
        PrecioText.Text = producto.PrecioVenta.ToString("0.##", CultureInfo.InvariantCulture);
        StockText.Text = producto.StockVenta.ToString();
        RequiereRecetaCheck.IsChecked = producto.RequiereReceta;
        EsMedicamentoCheck.IsChecked = producto.EsMedicamento;
        CategoriaCombo.SelectedValue = producto.CategoriaId;
        CancelarButton.Visibility = Visibility.Visible;
        MensajeText.Text = string.Empty;
    }

    private async void CambiarEstadoProducto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not { } item) return;

        var producto = (Producto)item.GetType().GetProperty("Producto")!.GetValue(item)!;

        try
        {
            await _service.CambiarEstadoAsync(producto.Id, !producto.Estado);
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MensajeText.Text = $"Error al cambiar el estado: {ex.Message}";
        }
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

    private void LimpiarFormulario()
    {
        _idEnEdicion = null;
        TituloFormularioText.Text = "Nuevo producto";
        DescripcionText.Text = PrecioText.Text = StockText.Text = string.Empty;
        RequiereRecetaCheck.IsChecked = false;
        EsMedicamentoCheck.IsChecked = false;
        CategoriaCombo.SelectedItem = null;
        CancelarButton.Visibility = Visibility.Collapsed;
        MensajeText.Text = string.Empty;
    }
}
