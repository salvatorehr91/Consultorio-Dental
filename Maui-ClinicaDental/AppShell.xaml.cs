using Maui_ClinicaDental.Pages;

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
            // 2026-11-19 (editar/eliminar): páginas nuevas para
            // administrar dentistas y tratamientos.
            Routing.RegisterRoute(nameof(DentistasPage), typeof(DentistasPage));
            Routing.RegisterRoute(nameof(TratamientosPage), typeof(TratamientosPage));
            // 2026-11-19: formularios de dentista y tratamiento (crear
            // y editar, con todos los campos a la vez en pantalla).
            Routing.RegisterRoute(nameof(NuevoDentistaPage), typeof(NuevoDentistaPage));
            Routing.RegisterRoute(nameof(NuevoTratamientoPage), typeof(NuevoTratamientoPage));
            // 2026-11-19: reportes del consultorio.
            Routing.RegisterRoute(nameof(ReportesPage), typeof(ReportesPage));
        }
    }
}