namespace Maui_ClinicaDental
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(NuevaCitaPage), typeof(NuevaCitaPage));
            Routing.RegisterRoute(nameof(NuevoPacientePage), typeof(NuevoPacientePage));
            Routing.RegisterRoute(nameof(PacientesPage), typeof(PacientesPage));
            Routing.RegisterRoute(nameof(CitasPage), typeof(CitasPage));
            Routing.RegisterRoute(nameof(PagosPage), typeof(PagosPage));
        }
    }
}