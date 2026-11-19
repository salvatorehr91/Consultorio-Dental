using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

// NuevoDentistaPage — formulario de dentista, con todos los campos
// a la vez en pantalla (2026-11-19: antes eran preguntas en pantalla
// una por una, y para cambiar un solo campo había que pasar por las
// 5). Misma página para crear y editar, como NuevoPacientePage: al
// navegar con ?dentistaId=1, Shell asigna DentistaIdQuery antes de
// OnAppearing y el ViewModel carga ese dentista del API para
// precargar el formulario. Sin ese parámetro, crea uno nuevo.
[QueryProperty(nameof(DentistaIdQuery), "dentistaId")]
public partial class NuevoDentistaPage : ContentPage
{
    public NuevoDentistaPage()
    {
        InitializeComponent();
        BindingContext = new NuevoDentistaViewModel();
    }

    public string DentistaIdQuery
    {
        set
        {
            if (BindingContext is NuevoDentistaViewModel vm
                && int.TryParse(value, out var id))
            {
                Title = "Editar dentista";
                _ = vm.CargarParaEditarAsync(id);
            }
        }
    }
}

public class NuevoDentistaViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Id del dentista que se está editando, o null si se está creando
    // uno nuevo. GuardarAsync decide entre POST y PUT con este valor.
    public int? DentistaIdEditar { get; private set; }
    public bool EnEdicion => DentistaIdEditar.HasValue;

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

    private string _especialidad = string.Empty;
    public string Especialidad
    {
        get => _especialidad;
        set { _especialidad = value; OnPropertyChanged(); }
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

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { _mensajeError = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneError)); }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    public NuevoDentistaViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        GuardarCommand = new Command(async () => await GuardarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    // Precarga el formulario con los datos del dentista. Si el API
    // no responde, se muestra el error y la página queda en modo
    // creación vacía, para no guardar sobre datos incompletos.
    public async Task CargarParaEditarAsync(int id)
    {
        var dentista = await _api.GetDentistaAsync(id);
        if (dentista is null)
        {
            MensajeError = _api.UltimoError ?? "No se pudo cargar el dentista para editar.";
            return;
        }

        DentistaIdEditar = dentista.DentistaId;
        Nombre = dentista.Nombre;
        Apellido = dentista.Apellido;
        Especialidad = dentista.Especialidad;
        Telefono = dentista.Telefono;
        Email = dentista.Email;
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

        if (string.IsNullOrWhiteSpace(Especialidad))
        {
            MensajeError = "La especialidad es obligatoria.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@'))
        {
            MensajeError = "El email no parece válido (debe contener @).";
            return;
        }

        var request = new ActualizarDentistaRequest(
            Nombre.Trim(),
            Apellido.Trim(),
            Especialidad.Trim(),
            string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
            string.IsNullOrWhiteSpace(Email) ? null : Email.Trim());

        if (DentistaIdEditar.HasValue)
        {
            var (ok, error) = await _api.ActualizarDentistaAsync(DentistaIdEditar.Value, request);
            if (!ok)
            {
                MensajeError = error ?? "El servidor no actualizó el dentista.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Dentista actualizado",
                $"{request.Nombre} {request.Apellido} — {request.Especialidad}.",
                "OK");
        }
        else
        {
            var (ok, error) = await _api.CrearDentistaAsync(request);
            if (!ok)
            {
                MensajeError = error ?? "El servidor no guardó el dentista.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Dentista creado",
                $"{request.Nombre} {request.Apellido} — {request.Especialidad}.",
                "OK");
        }

        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
