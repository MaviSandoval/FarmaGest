using System.Collections.Generic;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: consulta de obras sociales, planes, coberturas y afiliados (solo lectura).
public partial class ObrasSocialesPage : Page
{
    public ObrasSocialesPage()
    {
        InitializeComponent();

        AfiliadosGrid.ItemsSource = new List<AfiliadoFalso>
        {
            new() { Nombre = "Juan Pérez", NumeroAfiliado = "61-458921-02", ObraSocial = "OSDE", Plan = "Plan 210", Estado = "Activo" },
            new() { Nombre = "Laura Pérez", NumeroAfiliado = "15-907311-01", ObraSocial = "IOSCOR", Plan = "General", Estado = "Inactivo" },
        };

        ObrasSocialesList.ItemsSource = new List<ObraSocialFalsa>
        {
            new()
            {
                Nombre = "OSDE", Codigo = "OSDE01", Telefono = "0810-555-6733",
                Planes =
                {
                    new() { Nombre = "Plan 210", Descripcion = "Plan básico", Coberturas = { new() { Producto = "Amoxicilina 500 mg x 16", Porcentaje = 40 }, new() { Producto = "Losartán 50 mg x 30", Porcentaje = 40 } } },
                    new() { Nombre = "Plan 310", Descripcion = "Plan intermedio", Coberturas = { new() { Producto = "Amoxicilina 500 mg x 16", Porcentaje = 60 } } },
                }
            },
            new()
            {
                Nombre = "IOSCOR", Codigo = "IOS001", Telefono = "0379-442-0000",
                Planes =
                {
                    new() { Nombre = "General", Descripcion = "Afiliados obligatorios de la provincia", Coberturas = { new() { Producto = "Amoxicilina 500 mg x 16", Porcentaje = 50 }, new() { Producto = "Losartán 50 mg x 30", Porcentaje = 70 } } },
                }
            },
            new()
            {
                Nombre = "PAMI", Codigo = "PAMI01", Telefono = "138",
                Planes =
                {
                    new() { Nombre = "Único", Descripcion = "Jubilados y pensionados", Coberturas = { new() { Producto = "Losartán 50 mg x 30", Porcentaje = 100 }, new() { Producto = "Amoxicilina 500 mg x 16", Porcentaje = 80 }, new() { Producto = "Cetirizina 10 mg x 10", Porcentaje = 30 } } },
                }
            },
            new()
            {
                Nombre = "Swiss Medical", Codigo = "SMG001", Telefono = "0810-333-8876",
                Planes =
                {
                    new() { Nombre = "SMG20", Descripcion = "Plan joven", Coberturas = { new() { Producto = "Salbutamol 100 mcg x 200 dosis", Porcentaje = 50 } } },
                }
            },
        };

        ObrasSocialesList.SelectedIndex = 0;

        // Al cambiar de obra social, seleccionar su primer plan
        // (se hace en el Dispatcher para que primero se actualice la lista de planes)
        Loaded += (_, _) => SeleccionarPrimerPlan();
        ObrasSocialesList.SelectionChanged += (_, _) => SeleccionarPrimerPlan();
    }

    private void SeleccionarPrimerPlan()
    {
        Dispatcher.InvokeAsync(() => { PlanesList.SelectedIndex = 0; });
    }

    private class AfiliadoFalso
    {
        public string Nombre { get; set; } = "";
        public string NumeroAfiliado { get; set; } = "";
        public string ObraSocial { get; set; } = "";
        public string Plan { get; set; } = "";
        public string Estado { get; set; } = "";
    }

    private class ObraSocialFalsa
    {
        public string Nombre { get; set; } = "";
        public string Codigo { get; set; } = "";
        public string Telefono { get; set; } = "";
        public List<PlanFalso> Planes { get; } = new();
    }

    private class PlanFalso
    {
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public List<CoberturaFalsa> Coberturas { get; } = new();
    }

    private class CoberturaFalsa
    {
        public string Producto { get; set; } = "";
        public decimal Porcentaje { get; set; }
    }
}
