using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>
/// El Farmacéutico arma la venta (productos + receta + cobertura) y la envía a caja.
/// Los importes que se ven acá son una vista previa: el cálculo definitivo
/// (cobertura, stock y lote) lo hace VentaService al guardar.
/// </summary>
public partial class NuevaVentaPage : Page
{
    private const string OrigenReceta = "Receta";
    private const string OrigenLibre = "Libre";
    private static readonly CultureInfo Cultura = new("es-AR");

    private readonly VentaService _ventaService;
    private readonly RecetaService _recetaService;
    private readonly SesionUsuarioService _sesion;

    private readonly ObservableCollection<ItemDetalle> _detalle = new();
    private List<ProductoVendibleDto> _productos = new();
    private int? _recetaPreseleccionada;

    public NuevaVentaPage(VentaService ventaService, RecetaService recetaService, SesionUsuarioService sesion)
    {
        InitializeComponent();
        _ventaService = ventaService;
        _recetaService = recetaService;
        _sesion = sesion;

        DetalleGrid.ItemsSource = _detalle;
        Loaded += async (_, _) => await CargarAsync();
    }

    /// <summary>Se llama desde Validación de recetas ("Iniciar venta con esta receta").</summary>
    public void PreseleccionarReceta(int recetaId) => _recetaPreseleccionada = recetaId;

    private int UsuarioId =>
        _sesion.UsuarioActual?.Id ?? throw new InvalidOperationException("No hay una sesión iniciada.");

    private RecetaDto? RecetaElegida =>
        TipoRecetaRadio.IsChecked == true ? RecetaCombo.SelectedItem as RecetaDto : null;

    // =========================================================
    // CARGA INICIAL
    // =========================================================
    private async Task CargarAsync()
    {
        try
        {
            _productos = await _ventaService.ObtenerProductosVendiblesAsync();
            var recetas = await _recetaService.ObtenerValidadasDisponiblesAsync();

            RecetaCombo.ItemsSource = recetas;
            SinRecetasText.Visibility = recetas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var preseleccion = recetas.FirstOrDefault(r => r.Numero == _recetaPreseleccionada);

            if (preseleccion != null)
            {
                TipoRecetaRadio.IsChecked = true;
                RecetaCombo.SelectedItem = preseleccion;
            }
            else if (recetas.Count == 0)
            {
                TipoLibreRadio.IsChecked = true;
            }

            ActualizarProductosDisponibles();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar los datos de la venta.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // =========================================================
    // TIPO DE VENTA Y RECETA
    // =========================================================
    private void Tipo_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (TipoLibreRadio.IsChecked == true)
        {
            // En venta libre no hay receta: se sacan los ítems que venían de ella
            QuitarItemsDeReceta();
            RecetaCombo.SelectedItem = null;
        }

        ActualizarProductosDisponibles();
        ActualizarTotales();
    }

    private void Receta_Changed(object sender, SelectionChangedEventArgs e)
    {
        QuitarItemsDeReceta();

        if (RecetaCombo.SelectedItem is RecetaDto receta)
        {
            foreach (var medicamento in receta.Medicamentos)
            {
                // Si el producto ya estaba cargado a mano, pasa a ser parte de la receta
                var existente = _detalle.FirstOrDefault(i => i.ProductoId == medicamento.ProductoId);
                if (existente != null)
                    _detalle.Remove(existente);

                _detalle.Add(new ItemDetalle
                {
                    ProductoId = medicamento.ProductoId,
                    Producto = medicamento.Producto,
                    Origen = OrigenReceta,
                    Cantidad = medicamento.Cantidad,
                    PrecioUnitario = medicamento.PrecioUnitario,
                    PorcentajeCobertura = medicamento.Porcentaje
                });
            }
        }

        MensajeText.Text = string.Empty;
        ActualizarProductosDisponibles();
        ActualizarTotales();
    }

    /// <summary>
    /// Productos que se pueden agregar a mano:
    /// - Venta libre: solo los que NO requieren receta.
    /// - Con receta: los de venta libre + los medicamentos de la receta elegida.
    /// </summary>
    private void ActualizarProductosDisponibles()
    {
        var recetados = RecetaElegida?.Medicamentos.Select(m => m.ProductoId).ToHashSet() ?? new HashSet<int>();

        ProductoCombo.SelectedItem = null;
        ProductoCombo.Text = string.Empty;
        ProductoCombo.ItemsSource = _productos
            .Where(p => !p.RequiereReceta || recetados.Contains(p.Id))
            .ToList();
    }

    private void QuitarItemsDeReceta()
    {
        foreach (var item in _detalle.Where(i => i.Origen == OrigenReceta).ToList())
            _detalle.Remove(item);
    }

    // =========================================================
    // PRODUCTOS
    // =========================================================
    private void Agregar_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        // Si se escribió el nombre completo sin elegirlo de la lista, se busca igual
        var producto = ProductoCombo.SelectedItem as ProductoVendibleDto
            ?? _productos.FirstOrDefault(p => string.Equals(p.Descripcion, ProductoCombo.Text?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (producto == null)
        {
            MensajeText.Text = "Elegí un producto de la lista.";
            return;
        }

        var cantidad = ValidacionEntrada.LeerEntero(CantidadText.Text);
        if (cantidad is null or <= 0)
        {
            MensajeText.Text = "La cantidad debe ser un número entero mayor a cero.";
            return;
        }

        var receta = RecetaElegida;
        var existente = _detalle.FirstOrDefault(i => i.ProductoId == producto.Id);

        if (producto.RequiereReceta)
        {
            if (TipoLibreRadio.IsChecked == true)
            {
                MensajeText.Text = $"\"{producto.Descripcion}\" requiere receta: no se puede agregar en venta libre.";
                return;
            }

            if (receta == null || existente?.Origen != OrigenReceta)
            {
                MensajeText.Text = $"\"{producto.Descripcion}\" requiere receta y no figura en la receta elegida.";
                return;
            }
        }

        int total = (existente?.Cantidad ?? 0) + cantidad.Value;

        if (total > producto.Stock)
        {
            MensajeText.Text = $"Stock insuficiente de \"{producto.Descripcion}\": hay {producto.Stock} unidad(es).";
            return;
        }

        if (existente?.Origen == OrigenReceta)
        {
            var autorizado = receta?.Medicamentos.FirstOrDefault(m => m.ProductoId == producto.Id)?.Cantidad ?? 0;
            if (total > autorizado)
            {
                MensajeText.Text = $"La receta autoriza {autorizado} unidad(es) de \"{producto.Descripcion}\".";
                return;
            }
        }

        if (existente != null)
        {
            existente.Cantidad = total;
            DetalleGrid.Items.Refresh();
        }
        else
        {
            _detalle.Add(new ItemDetalle
            {
                ProductoId = producto.Id,
                Producto = producto.Descripcion,
                Origen = OrigenLibre,
                Cantidad = cantidad.Value,
                PrecioUnitario = producto.Precio,
                PorcentajeCobertura = 0
            });
        }

        ProductoCombo.SelectedItem = null;
        ProductoCombo.Text = string.Empty;
        CantidadText.Text = "1";
        ActualizarTotales();
    }

    private void Quitar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ItemDetalle item })
        {
            _detalle.Remove(item);
            ActualizarTotales();
        }
    }

    private void ActualizarTotales()
    {
        decimal subtotal = _detalle.Sum(i => i.Cantidad * i.PrecioUnitario);
        decimal descuento = _detalle.Sum(i => i.Descuento);

        CantidadItemsText.Text = _detalle.Sum(i => i.Cantidad).ToString(Cultura);
        SubtotalText.Text = subtotal.ToString("C", Cultura);
        DescuentoText.Text = "- " + descuento.ToString("C", Cultura);
        TotalText.Text = (subtotal - descuento).ToString("C", Cultura);
    }

    // =========================================================
    // ENVIAR A CAJA
    // =========================================================
    private async void EnviarACaja_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        if (TipoRecetaRadio.IsChecked == true && RecetaElegida == null)
        {
            MensajeText.Text = "Elegí una receta validada o cambiá a venta libre.";
            return;
        }

        if (_detalle.Count == 0)
        {
            MensajeText.Text = "Agregá al menos un producto.";
            return;
        }

        EnviarButton.IsEnabled = false;

        try
        {
            var items = _detalle.Select(i => new ItemVentaSolicitud(i.ProductoId, i.Cantidad)).ToList();
            int numero = await _ventaService.CrearVentaAsync(UsuarioId, RecetaElegida?.Numero, items);

            MessageBox.Show($"Venta N° {numero} enviada a caja. Queda pendiente de cobro.",
                "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService?.Navigate(App.Services.GetRequiredService<VentasFarmaceuticoPage>());
        }
        catch (Exception ex)
        {
            MensajeText.Text = ex.Message;
        }
        finally
        {
            EnviarButton.IsEnabled = true;
        }
    }

    private void Volver_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(App.Services.GetRequiredService<VentasFarmaceuticoPage>());

    // Fila del detalle (vista previa de la venta)
    private class ItemDetalle
    {
        public int ProductoId { get; set; }
        public string Producto { get; set; } = "";
        public string Origen { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeCobertura { get; set; }

        public decimal Descuento => Math.Round(Cantidad * PrecioUnitario * PorcentajeCobertura / 100m, 2);
        public decimal Subtotal => Cantidad * PrecioUnitario - Descuento;
        public string CoberturaTexto => PorcentajeCobertura > 0 ? $"{PorcentajeCobertura:0.##} %" : "—";
    }
}
