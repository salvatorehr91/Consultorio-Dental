using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

// NuevoTratamientoPage — formulario de tratamiento, con todos los
// campos a la vez en pantalla (2026-11-19: antes eran preguntas en
// pantalla una por una, y si se cancelaba alguna no se guardaba nada,
// así que parecía que no actualizaba). Misma página para crear y
// editar, como NuevoPacientePage y NuevoDentistaPage: al navegar con
// ?tratamientoId=1, Shell asigna TratamientoIdQuery antes de
// OnAppearing y el ViewModel carga ese tratamiento del API para
// precargar el formulario. Sin ese parámetro, crea uno nuevo.
[QueryProperty(nameof(TratamientoIdQuery), "tratamientoId")]
public partial class NuevoTratamientoPage : ContentPage
{
    public NuevoTratamientoPage()
    {
        InitializeComponent();
        BindingContext = new NuevoTratamientoViewModel();
    }

    public string TratamientoIdQuery
    {
        set
        {
            if (BindingContext is NuevoTratamientoViewModel vm
                && int.TryParse(value, out var id))
            {
                Title = "Editar tratamiento";
                _ = vm.CargarParaEditarAsync(id);
            }
        }
    }
}

public class NuevoTratamientoViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Id del tratamiento que se está editando, o null si se está
    // creando uno nuevo. GuardarAsync decide entre POST y PUT con
    // este valor.
    public int? TratamientoIdEditar { get; private set; }
    public bool EnEdicion => TratamientoIdEditar.HasValue;

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set { _nombre = value; OnPropertyChanged(); }
    }

    private string? _descripcion;
    public string? Descripcion
    {
        get => _descripcion;
        set { _descripcion = value; OnPropertyChanged(); }
    }

    // Precio y duración se escriben como texto en el formulario y se
    // convierten a número al guardar, aceptando punto o coma como
    // separador decimal (igual que el monto al registrar un pago).
    private string _precioTexto = string.Empty;
    public string PrecioTexto
    {
        get => _precioTexto;
        set { _precioTexto = value; OnPropertyChanged(); }
    }

    private string _duracionTexto = string.Empty;
    public string DuracionTexto
    {
        get => _duracionTexto;
        set { _duracionTexto = value; OnPropertyChanged(); }
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

    public NuevoTratamientoViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        GuardarCommand = new Command(async () => await GuardarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    // Precarga el formulario con los datos del tratamiento. Si el API
    // no responde, se muestra el error y la página queda en modo
    // creación vacía, para no guardar sobre datos incompletos.
    public async Task CargarParaEditarAsync(int id)
    {
        var tratamiento = await _api.GetTratamientoAsync(id);
        if (tratamiento is null)
        {
            MensajeError = _api.UltimoError ?? "No se pudo cargar el tratamiento para editar.";
            return;
        }

        TratamientoIdEditar = tratamiento.TratamientoId;
        Nombre = tratamiento.Nombre;
        Descripcion = tratamiento.Descripcion;
        PrecioTexto = tratamiento.Precio.ToString("0.00", CultureInfo.InvariantCulture);
        DuracionTexto = tratamiento.DuracionMinutos.ToString(CultureInfo.InvariantCulture);
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

        if ((!decimal.TryParse(PrecioTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) &&
             !decimal.TryParse(PrecioTexto, NumberStyles.Number, CultureInfo.CurrentCulture, out precio)) || precio < 0)
        {
            MensajeError = "El precio no es válido. Escribe solo números, por ejemplo 800 o 800.50.";
            return;
        }

        if (!int.TryParse(DuracionTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var duracion) || duracion <= 0)
        {
            MensajeError = "La duración no es válida. Escribe los minutos, por ejemplo 45.";
            return;
        }

        var request = new ActualizarTratamientoRequest(
            Nombre.Trim(),
            string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(),
            precio,
            duracion);

        if (TratamientoIdEditar.HasValue)
        {
            var (ok, error) = await _api.ActualizarTratamientoAsync(TratamientoIdEditar.Value, request);
            if (!ok)
            {
                MensajeError = error ?? "El servidor no actualizó el tratamiento.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Tratamiento actualizado",
                $"{request.Nombre} — ${request.Precio:0.00} · {request.DuracionMinutos} min.",
                "OK");
        }
        else
        {
            var (ok, error) = await _api.CrearTratamientoAsync(request);
            if (!ok)
            {
                MensajeError = error ?? "El servidor no guardó el tratamiento.";
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                "Tratamiento creado",
                $"{request.Nombre} — ${request.Precio:0.00} · {request.DuracionMinutos} min.",
                "OK");
        }

        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
