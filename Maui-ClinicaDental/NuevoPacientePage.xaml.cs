using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

public partial class NuevoPacientePage : ContentPage
{
	public NuevoPacientePage()
	{
		InitializeComponent(); 
		BindingContext = new NuevoPacienteViewModel();
    }
}

public class NuevoPacienteViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }

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

        var nuevoPaciente = new NuevoPacienteRequest(
            Nombre.Trim(),
            Apellido.Trim(),
            FechaNacimiento,
            string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
            string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim());

        var (ok, error) = await _api.CrearPacienteAsync(nuevoPaciente);
        if (!ok)
        {
            MensajeError = error ?? "El servidor no guardó el paciente.";
            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Paciente guardado",
            $"{nuevoPaciente.Nombre} {nuevoPaciente.Apellido} — Tel: {nuevoPaciente.Telefono ?? "—"}.",
            "OK");
        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

}