using Maui_ClinicaDental.Pages;
using Maui_ClinicaDental.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Maui_ClinicaDental
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 2026-11-20 (login): si hay sesión guardada, entra directo
            // al Shell; si no, pide usuario y contraseña primero.
            Page inicial = ApiService.HaySesionGuardada()
                ? new AppShell()
                : new LoginPage();
            return new Window(inicial);
        }
    }
}