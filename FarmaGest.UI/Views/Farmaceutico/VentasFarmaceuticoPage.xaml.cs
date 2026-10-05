using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI.Views.Farmaceutico;

/// <summary>Ventas que armó el Farmacéutico logueado y su estado en caja.</summary>
public partial class VentasFarmaceuticoPage : Page
{
    private readonly VentaService _ventaService;
    private readonly SesionUsuarioService _sesion;
    private List<VentaResumenDto> _ventas = new();

    public VentasFarmaceuticoPage(VentaService ventaService, SesionUsuarioService sesion)
    {
        InitializeComponent();
        _ventaService = ventaService;
        _sesion = sesion;

        FechaFiltroPicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CargarAsync();
    }

    private int UsuarioId =>
        _sesion.UsuarioActual?.Id ?? throw new InvalidOperationException("No hay una sesión iniciada.");

    private async Task CargarAsync()
    {
        try
        {
            _ventas = await _ventaService.ObtenerVentasAsync(UsuarioId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar las ventas.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var hoy = _ventas.Where(v => v.Fecha.Date == DateTime.Today).ToList();
        EnviadasHoyText.Text = hoy.Count.ToString();
        PendientesText.Text = _ventas.Count(v => v.Estado == VentaService.EstadoPendiente).ToString();
        FacturadasHoyText.Text = hoy.Count(v => v.Estado == VentaService.EstadoFacturada).ToString();
        AnuladasHoyText.Text = hoy.Count(v => v.Estado == VentaService.EstadoAnulada).ToString();

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarText.Text.Trim();
        DateTime? fecha = FechaFiltroPicker.SelectedDate;

        string? estado = EstadoCombo.SelectedIndex switch
        {
            1 => VentaService.EstadoPendiente,
            2 => VentaService.EstadoFacturada,
            3 => VentaService.EstadoAnulada,
            _ => null
        };

        VentasGrid.ItemsSource = _ventas
            .Where(v => fecha == null || v.Fecha.Date == fecha.Value.Date)
            .Where(v => estado == null || v.Estado == estado)
            .Where(v => texto.Length == 0 ||
                        v.Numero.ToString() == texto ||
                        v.Cliente.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro();
    }

    private void NuevaVenta_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(App.Services.GetRequiredService<NuevaVentaPage>());

    private async void VerDetalle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VentaResumenDto venta })
            return;

        try
        {
            var detalle = await _ventaService.ObtenerDetalleAsync(venta.Numero);

            var texto = new StringBuilder();
            texto.AppendLine($"Venta N° {venta.Numero} · {venta.Fecha:dd/MM/yyyy HH:mm}");
            texto.AppendLine($"{venta.Tipo} · {venta.Cliente}");
            texto.AppendLine($"Estado: {venta.Estado}" + (venta.MedioPago != "—" ? $" ({venta.MedioPago}, cobró {venta.Cajero})" : ""));
            texto.AppendLine();

            foreach (var d in detalle)
            {
                texto.AppendLine($"• {d.Producto} x{d.Cantidad}  $ {d.Subtotal:N2}" +
                                 (d.Descuento > 0 ? $"  (cobertura - $ {d.Descuento:N2})" : ""));
            }

            texto.AppendLine();
            texto.AppendLine($"Total: $ {venta.Total:N2}");

            MessageBox.Show(texto.ToString(), "Detalle de la venta", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar el detalle.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Anular_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VentaResumenDto venta })
            return;

        var respuesta = MessageBox.Show(
            $"¿Anular la venta N° {venta.Numero}? La receta queda libre para usarla en otra venta.",
            "FarmaGest", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            await _ventaService.AnularVentaAsync(venta.Numero);
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo anular la venta.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
