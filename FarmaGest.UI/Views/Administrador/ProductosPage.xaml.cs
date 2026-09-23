using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Administrador;

public partial class ProductosPage : Page
{
    private readonly ProductoService _service;
    private int? _idEnEdicion;

    public ProductosPage(ProductoService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        CategoriaCombo.ItemsSource = await _service.ObtenerCategoriasAsync();

        var productos = await _service.ObtenerTodosAsync();
        ProductosGrid.ItemsSource = productos.Select(p => new
        {
            Producto = p,
            p.Descripcion,
            p.Categoria,
            p.PrecioVenta,
            p.StockVenta,
            RequiereRecetaTexto = p.RequiereReceta ? "Sí" : "No",
            EstadoTexto = p.Estado ? "Activo" : "Inactivo",
            TextoAccionEstado = p.Estado ? "Dar de baja" : "Reactivar"
        }).ToList();
    }

    private async void GuardarButton_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        // Validación de tipos de dato: precio decimal (coma o punto) y stock entero
        var precioLeido = ValidacionEntrada.LeerDecimal(PrecioText.Text);
        var stockLeido = ValidacionEntrada.LeerEntero(StockText.Text);

        if (string.IsNullOrWhiteSpace(DescripcionText.Text) ||
            CategoriaCombo.SelectedValue is null ||
            precioLeido is null ||
            stockLeido is null)
        {
            MensajeText.Text = "Completá Descripción, Categoría, Precio (número con hasta 2 decimales) y Stock (número entero).";
            return;
        }

        if (precioLeido <= 0)
        {
            MensajeText.Text = "El precio debe ser mayor a cero.";
            return;
        }

        decimal precio = precioLeido.Value;
        int stock = stockLeido.Value;

        var categoriaId = ((Categoria)CategoriaCombo.SelectedItem).Id;

        try
        {
            if (_idEnEdicion is null)
            {
                await _service.CrearAsync(
                    DescripcionText.Text, precio, stock,
                    RequiereRecetaCheck.IsChecked == true, EsMedicamentoCheck.IsChecked == true, categoriaId);
            }
            else
            {
                await _service.EditarAsync(
                    _idEnEdicion.Value, DescripcionText.Text, precio, stock,
                    RequiereRecetaCheck.IsChecked == true, EsMedicamentoCheck.IsChecked == true, categoriaId);
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
        PrecioText.Text = producto.PrecioVenta.ToString(CultureInfo.InvariantCulture);
        StockText.Text = producto.StockVenta.ToString();
        RequiereRecetaCheck.IsChecked = producto.RequiereReceta;
        EsMedicamentoCheck.IsChecked = producto.EsMedicamento;
        CategoriaCombo.SelectedValue = producto.CategoriaId;
        CancelarButton.Visibility = Visibility.Visible;
    }

    private async void CambiarEstadoProducto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not { } item) return;

        var producto = (Producto)item.GetType().GetProperty("Producto")!.GetValue(item)!;
        await _service.CambiarEstadoAsync(producto.Id, !producto.Estado);
        await CargarDatosAsync();
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