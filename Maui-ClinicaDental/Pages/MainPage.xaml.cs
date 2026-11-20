using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is MainPageViewModel vm)
                await vm.CargarDesdeApiAsync();
        }
    }

    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _api;
        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime FechaHoy { get; } = DateTime.Today;
        public int CitasHoyCount => CitasHoy.Count;

        private int _totalPacientes = 128; // Ejemplo, hasta que el API responda.
        public int TotalPacientes
        {
            get => _totalPacientes;
            private set { _totalPacientes = value; OnPropertyChanged(); }
        }

        // Derivada de la colección: la hora más temprana de hoy.
        public string ProximaCitaHora => CitasHoy
            .Select(c => c.Hora)
            .OrderBy(h => h)
            .FirstOrDefault() ?? "--:--";

        private string _avisoApi = string.Empty;
        public string AvisoApi
        {
            get => _avisoApi;
            private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
        }
        public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

        public ObservableCollection<CitaHoy> CitasHoy { get; } = new()
        {
            new("09:00", "45 min", "María López", "Limpieza dental", "Dra. Ana Pérez", "Confirmada", "#0E7C7B"),
            new("10:00", "60 min", "Carlos Ruiz", "Ortodoncia — revisión", "Dr. Luis Gómez", "Programada", "#0F3556"),
            new("11:30", "30 min", "Sofía Mendoza", "Revisión general", "Dra. Ana Pérez", "Confirmada", "#0E7C7B"),
        };

        public ICommand BuscarPacienteCommand { get; }
        public ICommand NuevaCitaCommand { get; }
        public ICommand NuevoPacienteCommand { get; }
        public ICommand VerPacientesCommand { get; }
        public ICommand VerCitasCommand { get; }
        // 2026-11-19: accesos a las páginas nuevas de dentistas y
        // tratamientos, desde las acciones rápidas de MainPage.
        public ICommand VerDentistasCommand { get; }
        public ICommand VerTratamientosCommand { get; }

        public MainPageViewModel(ApiService? api = null)
        {
            _api = api ?? new ApiService();
            BuscarPacienteCommand = new Command<string>(async texto => await BuscarPacienteAsync(texto));
            NuevaCitaCommand = new Command(async () => await NavegarAsync("NuevaCitaPage", "Nueva cita"));
            NuevoPacienteCommand = new Command(async () => await NavegarAsync("NuevoPacientePage", "Nuevo paciente"));
            VerPacientesCommand = new Command(async () => await NavegarAsync("PacientesPage", "Pacientes"));
            VerCitasCommand = new Command(async () => await NavegarAsync("CitasPage", "Citas"));
            VerDentistasCommand = new Command(async () => await NavegarAsync("DentistasPage", "Dentistas"));
            VerTratamientosCommand = new Command(async () => await NavegarAsync("TratamientosPage", "Tratamientos"));
        }

        public async Task CargarDesdeApiAsync()
        {
            var citas = await _api.GetCitasHoyAsync();
            if (citas is null)
            {
                AvisoApi = "Sin conexión al API, mostrando datos de ejemplo. " + (_api.UltimoError ?? "");
                return;
            }

            AvisoApi = string.Empty;
            CitasHoy.Clear();
            foreach (var cita in citas.OrderBy(c => c.FechaHora))
                CitasHoy.Add(cita.ToCitaHoy());
            OnPropertyChanged(nameof(CitasHoyCount));
            OnPropertyChanged(nameof(ProximaCitaHora));

            var count = await _api.GetPacientesCountAsync();
            if (count.HasValue && _api.UltimoError is null)
                TotalPacientes = count.Value;
        }

        private async Task BuscarPacienteAsync(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                await NavegarAsync("PacientesPage", "Pacientes");
                return;
            }
            // PacientesPage recibe el texto por QueryProperty "buscar".
            await NavegarAsync($"PacientesPage?buscar={Uri.EscapeDataString(texto)}", "Pacientes");
        }

        private async Task NavegarAsync(string ruta, string nombrePagina)
        {
            try
            {
                if (Shell.Current is null)
                {
                    await MostrarAlertaAsync(
                        "Navegación no disponible",
                        "Shell.Current es null. En App.xaml.cs debe ser:\nMainPage = new AppShell();\n(no new MainPage()).");
                    return;
                }

                await Shell.Current.GoToAsync(ruta);
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    $"No se pudo abrir {nombrePagina}",
                    $"Ruta '{ruta}': {ex.Message}\nRevisa que esté registrada en AppShell.xaml.cs con Routing.RegisterRoute.");
            }
        }

        private static Task MostrarAlertaAsync(string titulo, string mensaje)
        {
            var pagina = Shell.Current ?? Application.Current?.MainPage;
            return pagina is null
                ? Task.CompletedTask
                : pagina.DisplayAlertAsync(titulo, mensaje, "OK");
        }

        private void OnPropertyChanged([CallerMemberName] string? nombre = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

    }
}
