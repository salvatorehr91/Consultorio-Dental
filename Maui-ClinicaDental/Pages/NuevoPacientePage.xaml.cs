using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// 2026-11-19 (editar): la misma página crea y edita. Al navegar
// con ?pacienteId=1, Shell asigna PacienteIdQuery antes de
// OnAppearing y el ViewModel carga ese paciente del API para
// precargar el formulario. Sin ese parámetro, crea uno nuevo.
[QueryProperty(nameof(PacienteIdQuery), "pacienteId")]
public partial class NuevoPacientePage : ContentPage
{
	public NuevoPacientePage()
	{
		InitializeComponent();
		BindingContext = new NuevoPacienteViewModel();
    }

    public string PacienteIdQuery
    {
        set
        {
            if (BindingContext is NuevoPacienteViewModel vm
                && int.TryParse(value, out var id))
            {
                Title = "Editar paciente";
                _ = vm.CargarParaEditarAsync(id);
            }
        }
    }
}

public class NuevoPacienteViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Id del paciente que se está editando, o null si se está creando
    // uno nuevo. GuardarAsync decide entre POST y PUT con este valor.
    public int? PacienteIdEditar { get; private set; }

    // Título del botón: "Guardar paciente" al crear, "Guardar cambios"
    // al editar. La XAML no cambia: el texto del botón se deja fijo y
    // la alerta final dice si se creó o se actualizó.
    public bool EnEdicion => PacienteIdEditar.HasValue;

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set { _nombre = value; OnPropertyChanged(); }
    }

    private string _apellido = string.Empty;
    public string Apellido
    {
        get => _apellido;
        set { _apellido = value; OnPropertyChanged(); }
    }

    private string? _telefono;
    public string? Telefono
    {
        get => _telefono;
        set { _telefono = value; OnPropertyChanged(); }
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    private string? _direccion;
    public string? Direccion
    {
        get => _direccion;
        set { _direccion = value; OnPropertyChanged(); }
    }

    private DateTime _fechaNacimiento = new DateTime(1990, 1, 1);
    public DateTime FechaNacimiento
    {
        get => _fechaNacimiento;
        set { _fechaNacimiento = value; OnPropertyChanged(); }
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { _mensajeError = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneError)); }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    public NuevoPacienteViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        GuardarCommand = new Command(async () => await GuardarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    // Precarga el formulario con los datos del paciente. Si el API
    // no responde, se muestra el error y la página queda en modo
    // creación vacía, para no guardar sobre datos incompletos.
    public async Task CargarParaEditarAsync(int id)
    {
        var paciente = await _api.GetPacienteAsync(id);
        if (paciente is null)
        {
            MensajeError = _api.UltimoError ?? "No se pudo cargar el paciente para editar.";
            return;
        }

        PacienteIdEditar = paciente.PacienteId;
        Nombre = paciente.Nombre;
        Apellido = paciente.Apellido;
        FechaNacimiento = paciente.FechaNacimiento ?? new DateTime(1990, 1, 1);
        Telefono = paciente.Telefono;
        Email = paciente.Email;
        Direccion = paciente.Direccion;
        OnPropertyChanged(nameof(EnEdicion));
    }

    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MensajeError = "El nombre es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Apellido))
        {
            MensajeError = "El apellido es obligatorio.";
            return;
        }

        if (FechaNacimiento.Date > DateTime.Today)
        {
            MensajeError = "La fecha de nacimiento no puede estar en el futuro.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@'))
        {
            MensajeError = "El email no parece válido (debe contener @).";
            return;
        }

        // Mismos campos para crear y para editar: el servidor usa el
        // mismo contrato en POST y en PUT (CrearPacienteDto /
        // ActualizarPacienteDto). NuevoPacienteRequest ya existe y
        // coincide campo por campo, así que se reutiliza en ambos.
        var paciente = new NuevoPacienteRequest(
            Nombre.Trim(),
            Apellido.Trim(),
            FechaNacimiento,
            string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
            string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim());

        if (PacienteIdEditar.HasValue)
        {
            var (ok, error) = await _api.ActualizarPacienteAsync(
                PacienteIdEditar.Value,
                new ActualizarPacienteRequest(
                    paciente.Nombre, paciente.Apellido, paciente.FechaNacimiento,
                    paciente.Telefono, paciente.Email, paciente.Direccion));
            if (!ok)
            {
                MensajeError = error ?? "El servidor no actualizó el paciente.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Paciente actualizado",
                $"{paciente.Nombre} {paciente.Apellido} — Tel: {paciente.Telefono ?? "—"}.",
                "OK");
        }
        else
        {
            var (ok, error) = await _api.CrearPacienteAsync(paciente);
            if (!ok)
            {
                MensajeError = error ?? "El servidor no guardó el paciente.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Paciente guardado",
                $"{paciente.Nombre} {paciente.Apellido} — Tel: {paciente.Telefono ?? "—"}.",
                "OK");
        }

        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

}