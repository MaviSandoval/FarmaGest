using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Compartido;

/// <summary>
/// Tablero de resumen de Reportes: indicadores del período (con variación contra el
/// período anterior), evolución de ventas, obras sociales, top de productos,
/// categorías y medios de pago. Solo muestra datos: la página le pasa el resumen ya calculado.
/// </summary>
public partial class ResumenReportes : UserControl
{
    private static readonly Brush Positivo = new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x6C));
    private static readonly Brush Negativo = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x50));
    private static readonly Brush Neutro = new SolidColorBrush(Color.FromRgb(0x6B, 0x7B, 0x79));

    public ResumenReportes()
    {
        InitializeComponent();
    }

    public void Mostrar(ResumenReportesDto r)
    {
        SinDatosAviso.Visibility = r.Ventas == 0 ? Visibility.Visible : Visibility.Collapsed;

        // ---------- Indicadores ----------
        FacturacionText.Text = $"$ {r.Facturacion:N0}";

        if (r.Variacion is decimal variacion)
        {
            VariacionText.Text = $"{(variacion >= 0 ? "▲" : "▼")} {System.Math.Abs(variacion):N1} % respecto al período anterior";
            VariacionText.Foreground = variacion >= 0 ? Positivo : Negativo;
        }
        else
        {
            VariacionText.Text = "Sin ventas en el período anterior";
            VariacionText.Foreground = Neutro;
        }

        TicketText.Text = $"$ {r.TicketPromedio:N0}";
        VentasText.Text = $"{r.Ventas} venta(s) cobrada(s) en el período";

        UnidadesText.Text = r.Unidades.ToString("N0");
        UnidadesRecetaText.Text = $"{r.UnidadesConReceta:N0} con receta";

        ConObraSocialText.Text = $"{r.PorcentajeConObraSocial:N0} %";
        CoberturaText.Text = $"Cobertura descontada: $ {r.CoberturaDescontada:N0}";

        // ---------- Evolución ----------
        EvolucionDetalleText.Text = $"facturación {r.TituloEvolucion}";
        Graficos.EvolucionDestacada(EvolucionChart,
            r.Evolucion.Select(p => p.Etiqueta),
            r.Evolucion.Select(p => (double)p.Total));

        // ---------- Listas ----------
        ObrasSocialesList.ItemsSource = r.ObrasSociales;
        ObrasSocialesVacio.Visibility = r.ObrasSociales.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        ProductosList.ItemsSource = r.TopProductos;
        ProductosVacio.Visibility = r.TopProductos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        Graficos.Dona(CategoriasChart, r.Categorias.Select(c => (c.Nombre, (double)c.Importe)));

        MediosPagoList.ItemsSource = r.MediosPago;
        MediosVacio.Visibility = r.MediosPago.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
