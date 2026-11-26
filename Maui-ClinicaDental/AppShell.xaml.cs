using Maui_ClinicaDental.Pages;
using Maui_ClinicaDental.Service;

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
            // 2026-11-20: login de administrador.
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            // 2026-11-20: administración de usuarios (solo Admin).
            Routing.RegisterRoute(nameof(UsuariosPage), typeof(UsuariosPage));
            Routing.RegisterRoute(nameof(NuevoUsuarioPage), typeof(NuevoUsuarioPage));
            // 2026-11-21: bitácora de cambios (solo Admin).
            Routing.RegisterRoute(nameof(BitacoraPage), typeof(BitacoraPage));
        }

        // 2026-11-20 (menú): acceso directo a una pantalla. La
        // misma acción que los botones de MainPage: navegación
        // relativa con el nombre de la ruta, tal como lo hace
        // NavegarAsync en MainPageViewModel (GoToAsync("CitasPage"),
        // sin "//"). El nombre de la pantalla viene en
        // CommandParameter del menú.
        // 2026-11-20 (corrección): el menú lateral se cierra ANTES
        // de navegar. Navegar con el menú abierto cerraba la app
        // en Android. Si la ruta falla, se muestra el error en vez
        // de cerrar la app, igual que los botones de MainPage.
        // "Inicio" vuelve a MainPage con PopToRootAsync: saca de la
        // pila todas las pantallas hasta llegar a la principal.
        private async void IrPagina_Clicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            if (sender is not MenuItem item || item.CommandParameter is not string pagina)
                return;

            try
            {
                if (pagina == "MainPage")
                    await Shell.Current.Navigation.PopToRootAsync();
                else
                    await Shell.Current.GoToAsync(pagina);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    $"No se pudo abrir {item.Text}",
                    $"Ruta '{pagina}': {ex.Message}\nRevisa que esté registrada en AppShell.xaml.cs con Routing.RegisterRoute.",
                    "OK");
            }
        }

        // 2026-11-21 (bitácora): abre la bitácora solo si el
        // logueado es Admin; si no, avisa y no abre (el servidor
        // también lo bloquea con 403).
        private async void VerBitacora_Clicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            await ApiService.CargarSesionGuardadaAsync();
            if (!new ApiService().EsAdmin)
            {
                await DisplayAlertAsync(
                    "Solo Admin",
                    "La bitácora es solo para el administrador.",
                    "OK");
                return;
            }
            await Shell.Current.GoToAsync(nameof(BitacoraPage));
        }

        // 2026-11-25 (actualización): revisión manual. Cierra el
        // menú primero, como los demás handlers.
        private async void BuscarActualizacion_Clicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            await ActualizacionService.RevisarAsync(manual: true);
        }

        // 2026-11-20 (usuarios): abre la administración de
        // usuarios solo si el logueado es Admin; si no, avisa y no
        // abre (el servidor también lo bloquea con 403).
        private async void AdministrarUsuarios_Clicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            await ApiService.CargarSesionGuardadaAsync();
            if (!new ApiService().EsAdmin)
            {
                await DisplayAlertAsync(
                    "Solo Admin",
                    "Administrar usuarios es solo para el administrador.",
                    "OK");
                return;
            }
            await Shell.Current.GoToAsync(nameof(UsuariosPage));
        }

        // 2026-11-20 (login): confirma, borra el token y vuelve a
        // la página de login.
        private async void CerrarSesion_Clicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            var confirmar = await DisplayAlertAsync(
                "Cerrar sesión", "¿Cerrar la sesión?", "Sí", "Cancelar");
            if (!confirmar) return;

            await ApiService.CerrarSesionAsync();
            Application.Current!.MainPage = new LoginPage();
        }
    }
}