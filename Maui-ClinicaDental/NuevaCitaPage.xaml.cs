using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

public partial class NuevaCitaPage : ContentPage
{
    public NuevaCitaPage()
    {
        InitializeComponent();
        BindingContext = new NuevaCitaViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NuevaCitaViewModel vm)
            await vm.CargarCatalogosDesdeApiAsync();
    }
}


public class NuevaCitaViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Datos de ejemplo, iguales a los del script ConsultorioDental.sql.
    // Se reemplazan por el API en CargarCatalogosDesdeApiAsync.
    public ObservableCollection<OpcionPersona> Pacientes { get; } = new()
    {
        new(1, "María López"),
        new(2, "Carlos Ruiz"),
        new(3, "Sofía Mendoza"),
    };

    public ObservableCollection<OpcionPersona> Dentistas { get; } = new()
    {
        new(1, "Dra. Ana Pérez"),
        new(2, "Dr. Luis Gómez"),
    };

    public ObservableCollection<OpcionTratamiento> Tratamientos { get; } = new()
    {
        new(1, "Limpieza dental", 800.00m, 45),
        new(2, "Revisión general", 500.00m, 30),
        new(3, "Ortodoncia — revisión", 1200.00m, 60),
    };

    public ObservableCollection<int> Duraciones { get; } = new() { 15, 30, 45, 60, 90 };

    // Estados permitidos por el CHECK de dbo.Citas.Estado.
    public ObservableCollection<string> Estados { get; } = new()
    {
        "Programada", "Confirmada", "Completada", "Cancelada", "NoAsistio",
    };

    private OpcionPersona? _pacienteSeleccionado;
    public OpcionPersona? PacienteSeleccionado
    {
        get => _pacienteSeleccionado;
        set { _pacienteSeleccionado = value; OnPropertyChanged(); }
    }

    private OpcionPersona? _dentistaSeleccionado;
    public OpcionPersona? DentistaSeleccionado
    {
        get => _dentistaSeleccionado;
        set { _dentistaSeleccionado = value; OnPropertyChanged(); }
    }

    private OpcionTratamiento? _tratamientoSeleccionado;
    public OpcionTratamiento? TratamientoSeleccionado
    {
        get => _tratamientoSeleccionado;
        set
        {
            _tratamientoSeleccionado = value;
            OnPropertyChanged();
            if (value is not null) DuracionMinutos = value.DuracionMinutos;
            OnPropertyChanged(nameof(TratamientoResumen));
        }
    }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set { _fecha = value; OnPropertyChanged(); }
    }

    private TimeSpan _hora = new(9, 0, 0);
    public TimeSpan Hora
    {
        get => _hora;
        set { _hora = value; OnPropertyChanged(); }
    }

    private int _duracionMinutos = 30;
    public int DuracionMinutos
    {
        get => _duracionMinutos;
        set { _duracionMinutos = value; OnPropertyChanged(); }
    }

    private string _estado = "Programada";
    public string Estado
    {
        get => _estado;
        set { _estado = value; OnPropertyChanged(); }
    }

    public string? Motivo { get; set; }
    public string? Notas { get; set; }

    public string TratamientoResumen => TratamientoSeleccionado is null
        ? "Sin tratamiento seleccionado."
        : $"{TratamientoSeleccionado.Nombre} — ${TratamientoSeleccionado.Precio:0.00} · {TratamientoSeleccionado.DuracionMinutos} min";

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { _mensajeError = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneError)); }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    public NuevaCitaViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        GuardarCommand = new Command(async () => await GuardarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task CargarCatalogosDesdeApiAsync()
    {
        var pacientes = await _api.GetPacientesAsync();
        if (pacientes is not null)
        {
            Pacientes.Clear();
            foreach (var p in pacientes) Pacientes.Add(p.ToOpcionPersona());
        }

        var dentistas = await _api.GetDentistasAsync();
        if (dentistas is not null)
        {
            Dentistas.Clear();
            foreach (var d in dentistas) Dentistas.Add(d.ToOpcionPersona());
        }

        var tratamientos = await _api.GetTratamientosAsync();
        if (tratamientos is not null)
        {
            Tratamientos.Clear();
            foreach (var t in tratamientos) Tratamientos.Add(t.ToOpcionTratamiento());
        }

        if (pacientes is null && dentistas is null && tratamientos is null)
            MensajeError = "Sin conexión al API, usando catálogos de ejemplo.";
    }

    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;

        if (PacienteSeleccionado is null)
        {
            MensajeError = "Elige un paciente para la cita.";
            return;
        }

        if (DentistaSeleccionado is null)
        {
            MensajeError = "Elige un dentista para la cita.";
            return;
        }

        var fechaHora = Fecha.Date + Hora;
        if (fechaHora < DateTime.Now.AddMinutes(-1))
        {
            MensajeError = "La fecha y hora no pueden estar en el pasado.";
            return;
        }

        var nuevaCita = new NuevaCitaRequest(
            PacienteSeleccionado.Id,
            DentistaSeleccionado.Id,
            fechaHora,
            DuracionMinutos,
            Estado,
            string.IsNullOrWhiteSpace(Motivo) ? TratamientoSeleccionado?.Nombre : Motivo,
            Notas,
            TratamientoSeleccionado?.Id);

        var (ok, error) = await _api.CrearCitaAsync(nuevaCita);
        if (!ok)
        {
            MensajeError = error ?? "El servidor no guardó la cita.";
            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Cita guardada",
            $"{PacienteSeleccionado.NombreCompleto} con {DentistaSeleccionado.NombreCompleto}, " +
            $"{nuevaCita.FechaHora:dddd dd MMMM yyyy HH:mm} ({nuevaCita.DuracionMinutos} min).",
            "OK");
        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}