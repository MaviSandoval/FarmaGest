using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>Historial de ventas: quién la armó, quién la cobró y cómo se pagó.</summary>
public partial class VentasPage : Page
{
    private readonly VentaService _ventaService;
    private List<VentaResumenDto> _ventas = new();

    public VentasPage(VentaService ventaService)
    {
        InitializeComponent();
        _ventaService = ventaService;

        FechaPicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            _ventas = await _ventaService.ObtenerVentasAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar las ventas.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var hoy = _ventas.Where(v => v.Fecha.Date == DateTime.Today).ToList();
        VentasHoyText.Text = hoy.Count(v => v.Estado != VentaService.EstadoAnulada).ToString();
        FacturadoHoyText.Text = $"$ {hoy.Where(v => v.Estado == VentaService.EstadoFacturada).Sum(v => v.Total):N2}";
        PendientesText.Text = _ventas.Count(v => v.Estado == VentaService.EstadoPendiente).ToString();
        AnuladasHoyText.Text = hoy.Count(v => v.Estado == VentaService.EstadoAnulada).ToString();

        AplicarFiltro();
    }

    private List<VentaResumenDto> VentasFiltradas()
    {
        string texto = BuscarText.Text.Trim();
        DateTime? fecha = FechaPicker.SelectedDate;

        string? estado = EstadoCombo.SelectedIndex switch
        {
            1 => VentaService.EstadoPendiente,
            2 => VentaService.EstadoFacturada,
            3 => VentaService.EstadoAnulada,
            _ => null
        };

        bool? conReceta = TipoCombo.SelectedIndex switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        return _ventas
            .Where(v => fecha == null || v.Fecha.Date == fecha.Value.Date)
            .Where(v => estado == null || v.Estado == estado)
            .Where(v => conReceta == null || v.TieneReceta == conReceta)
            .Where(v => texto.Length == 0 ||
                        v.Numero.ToString() == texto ||
                        v.Cliente.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        v.Responsable.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void AplicarFiltro() => VentasGrid.ItemsSource = VentasFiltradas();

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro();
    }

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
            texto.AppendLine($"Armó: {venta.Responsable}");
            texto.AppendLine($"Estado: {venta.Estado}" +
                             (venta.Estado == VentaService.EstadoFacturada ? $" · {venta.MedioPago} · cobró {venta.Cajero}" : ""));
            texto.AppendLine();

            foreach (var d in detalle)
            {
                texto.AppendLine($"• {d.Producto} x{d.Cantidad}  $ {d.Subtotal:N2}" +
                                 (d.Descuento > 0 ? $"  (cobertura - $ {d.Descuento:N2})" : ""));
            }

            texto.AppendLine();
            texto.AppendLine($"Subtotal: $ {venta.Subtotal:N2}");
            texto.AppendLine($"Cobertura obra social: - $ {venta.Descuento:N2}");
            texto.AppendLine($"Total: $ {venta.Total:N2}");

            MessageBox.Show(texto.ToString(), "Detalle de la venta", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar el detalle.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Exportar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ruta = ExportadorCsv.Exportar<VentaResumenDto>("Ventas", VentasFiltradas(),
                ("N° venta", v => v.Numero),
                ("Fecha", v => v.Fecha),
                ("Tipo", v => v.Tipo),
                ("Paciente / cliente", v => v.Cliente),
                ("Armó", v => v.Responsable),
                ("Estado", v => v.Estado),
                ("Medio de pago", v => v.MedioPago),
                ("Cobró", v => v.Cajero),
                ("Subtotal", v => v.Subtotal),
                ("Cobertura", v => v.Descuento),
                ("Total", v => v.Total));

            if (ruta != null)
                MessageBox.Show($"Listado exportado en:\n{ruta}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo exportar.\n\n{ex.Message}", "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
