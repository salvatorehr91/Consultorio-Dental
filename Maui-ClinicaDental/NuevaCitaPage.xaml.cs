using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

// 2026-11-19 (editar): la misma página crea y edita. Al navegar
// con ?citaId=1, Shell asigna CitaIdQuery antes de OnAppearing y el
// ViewModel carga esa cita del API para precargar el formulario.
// Sin ese parámetro, crea una cita nueva.
[QueryProperty(nameof(CitaIdQuery), "citaId")]
public partial class NuevaCitaPage : ContentPage
{
    public NuevaCitaPage()
    {
        InitializeComponent();
        BindingContext = new NuevaCitaViewModel();
    }

    public string CitaIdQuery
    {
        set
        {
            if (BindingContext is NuevaCitaViewModel vm
                && int.TryParse(value, out var id))
            {
                Title = "Editar cita";
                _ = vm.CargarParaEditarAsync(id);
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NuevaCitaViewModel vm && !vm.EnEdicion)
            // En edición no se recargan: CargarParaEditarAsync ya cargó
            // los catálogos, y recargarlos reemplazaría las opciones
            // por instancias nuevas y perdería la selección precargada.
            await vm.CargarCatalogosDesdeApiAsync();
    }
}


public class NuevaCitaViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Id de la cita que se está editando, o null si se está creando
    // una nueva. GuardarAsync decide entre POST y PUT con este valor.
    public int? CitaIdEditar { get; private set; }
    public bool EnEdicion => CitaIdEditar.HasValue;

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

    // 2026-11-19: con notificación de cambio, para que al precargar
    // el formulario en edición la UI muestre el motivo y las notas de
    // la cita (con auto-properties, el valor quedaba en el ViewModel
    // pero el Entry/Editor seguía vacío).
    private string? _motivo;
    public string? Motivo
    {
        get => _motivo;
        set { _motivo = value; OnPropertyChanged(); }
    }

    private string? _notas;
    public string? Notas
    {
        get => _notas;
        set { _notas = value; OnPropertyChanged(); }
    }

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

    // Precarga el formulario con los datos de la cita. Primero carga
    // los catálogos, porque los pickers necesitan las opciones antes
    // de poder seleccionar el paciente, dentista y tratamiento de la
    // cita (se buscan por Id, que el API ya devuelve en CitaDto).
    public async Task CargarParaEditarAsync(int id)
    {
        await CargarCatalogosDesdeApiAsync();

        var cita = await _api.GetCitaAsync(id);
        if (cita is null)
        {
            MensajeError = _api.UltimoError ?? "No se pudo cargar la cita para editar.";
            return;
        }

        CitaIdEditar = cita.CitaId;
        PacienteSeleccionado = Pacientes.FirstOrDefault(p => p.Id == cita.PacienteId);
        DentistaSeleccionado = Dentistas.FirstOrDefault(d => d.Id == cita.DentistaId);
        Fecha = cita.FechaHora.Date;
        Hora = cita.FechaHora.TimeOfDay;
        Estado = cita.Estado;
        Motivo = cita.Motivo;
        Notas = cita.Notas;
        // El tratamiento se asigna ANTES que la duración: su setter
        // pone DuracionMinutos en la duración del catálogo, y si la
        // cita tenía una duración distinta (editada a mano), se
        // perdería. Al asignar la duración después, queda la de la cita.
        TratamientoSeleccionado = cita.TratamientoId.HasValue
            ? Tratamientos.FirstOrDefault(t => t.Id == cita.TratamientoId.Value)
            : null;
        DuracionMinutos = cita.DuracionMinutos;
        OnPropertyChanged(nameof(EnEdicion));
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
        // Al crear, la fecha no puede estar en el pasado. Al editar
        // no se aplica: una cita de ayer también se puede editar (por
        // ejemplo, para cambiar su estado o corregir sus notas).
        if (!EnEdicion && fechaHora < DateTime.Now.AddMinutes(-1))
        {
            MensajeError = "La fecha y hora no pueden estar en el pasado.";
            return;
        }

        var motivo = string.IsNullOrWhiteSpace(Motivo) ? TratamientoSeleccionado?.Nombre : Motivo;

        if (EnEdicion)
        {
            var (ok, error) = await _api.ActualizarCitaAsync(
                CitaIdEditar!.Value,
                new ActualizarCitaRequest(
                    PacienteSeleccionado.Id,
                    DentistaSeleccionado.Id,
                    fechaHora,
                    DuracionMinutos,
                    Estado,
                    motivo,
                    Notas,
                    TratamientoSeleccionado?.Id));
            if (!ok)
            {
                MensajeError = error ?? "El servidor no actualizó la cita.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Cita actualizada",
                $"{PacienteSeleccionado.NombreCompleto} con {DentistaSeleccionado.NombreCompleto}, " +
                $"{fechaHora:dddd dd MMMM yyyy HH:mm} ({DuracionMinutos} min).",
                "OK");
        }
        else
        {
            var nuevaCita = new NuevaCitaRequest(
                PacienteSeleccionado.Id,
                DentistaSeleccionado.Id,
                fechaHora,
                DuracionMinutos,
                Estado,
                motivo,
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
        }

        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}