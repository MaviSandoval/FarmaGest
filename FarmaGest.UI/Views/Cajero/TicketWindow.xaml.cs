using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using FarmaGest.Negocio.Servicios;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Cajero;

/// <summary>
/// Ticket de una venta cobrada, con los datos reales de la base
/// (FacturacionService.ObtenerTicketAsync). Se abre al cobrar y desde
/// "Ventas cobradas" / "Resumen de caja".
/// </summary>
public partial class TicketWindow : FluentWindow
{
    private static readonly CultureInfo Cultura = new("es-AR");

    public TicketWindow(TicketDto ticket)
    {
        InitializeComponent();
        Mostrar(ticket);
    }

    /// <summary>Busca el ticket de la venta y lo abre. Muestra un aviso si la venta todavía no se cobró.</summary>
    public static async Task AbrirAsync(FacturacionService facturacionService, int ventaId, Window? owner)
    {
        try
        {
            var ticket = await facturacionService.ObtenerTicketAsync(ventaId);

            if (ticket == null)
            {
                System.Windows.MessageBox.Show($"La venta N° {ventaId} todavía no tiene un cobro registrado.", "FarmaGest",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            new TicketWindow(ticket) { Owner = owner }.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo cargar el ticket.\n\n{ex.Message}", "FarmaGest",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void Mostrar(TicketDto t)
    {
        NumeroText.Text = $"Ticket N° {t.NumeroTicket}";
        FechaText.Text = $"Fecha: {t.Fecha:dd/MM/yyyy HH:mm}";
        VentaText.Text = $"Venta N° {t.VentaId}";
        PreparoText.Text = $"Preparó: {t.Preparo}";
        CobroText.Text = $"Cobró:   {t.Cobro}";

        DetalleList.ItemsSource = t.Items;

        SubtotalText.Text = t.Subtotal.ToString("C", Cultura);
        DescuentoText.Text = "- " + t.Descuento.ToString("C", Cultura);
        DescuentoFila.Visibility = t.Descuento > 0 ? Visibility.Visible : Visibility.Collapsed;
        TotalText.Text = t.Total.ToString("C", Cultura);

        MedioText.Text = t.MetodoPago;

        // "Paga con" y "Vuelto" solo tienen sentido en efectivo
        var efectivo = t.EsEfectivo ? Visibility.Visible : Visibility.Collapsed;
        PagaConFila.Visibility = efectivo;
        VueltoFila.Visibility = efectivo;
        PagaConText.Text = t.PagaCon.ToString("C", Cultura);
        VueltoText.Text = t.MontoCambio.ToString("C", Cultura);

        bool conObraSocial = t.ObraSocial != null;
        ObraSocialText.Visibility = conObraSocial ? Visibility.Visible : Visibility.Collapsed;
        AfiliadoText.Visibility = conObraSocial ? Visibility.Visible : Visibility.Collapsed;
        ObraSocialText.Text = $"Obra social: {t.ObraSocial} · {t.Plan}";
        AfiliadoText.Text = $"Afiliado: {t.Afiliado}";
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

    private void Imprimir_Click(object sender, RoutedEventArgs e)
    {
        // Imprime el comprobante tal como se ve (incluye "Guardar como PDF" en el diálogo de Windows)
        var dialogo = new System.Windows.Controls.PrintDialog();
        if (dialogo.ShowDialog() == true)
            dialogo.PrintVisual(TicketContenido, $"Ticket {NumeroText.Text}");
    }
}
