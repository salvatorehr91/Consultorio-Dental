using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// BitacoraPage — bitácora de cambios (2026-11-21). Solo la abre un
// Admin (el menú del Shell lo revisa y el servidor devuelve 403 si
// no lo es). Sin datos de ejemplo: son registros reales del API.
// La fecha se guarda en UTC en el servidor y aquí se muestra en
// hora local del consultorio.
public partial class BitacoraPage : ContentPage
{
    public BitacoraPage()
    {
        InitializeComponent();
        BindingContext = new BitacoraViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BitacoraViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class BitacoraItem
{
    public required DateTime FechaHora { get; init; }
    public required string UsuarioNombre { get; init; }
    public required string Accion { get; init; }
    public required string Entidad { get; init; }
    public required int EntidadId { get; init; }
    public required string Detalle { get; init; }

    public string FechaTexto => FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string Descripcion => $"{Entidad} #{EntidadId} — {Detalle}";
    public string AccionColor => Accion switch
    {
        "Eliminar" or "Desactivar" => "#B3261E",
        "Crear" or "Reactivar" => "#0E7C7B",
        _ => "#0F3556"
    };
}

public class BitacoraViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BitacoraItem> Registros { get; } = [];

    public List<string> Entidades { get; } =
        ["Todas", "Paciente", "Cita", "Pago", "Dentista", "Tratamiento", "Usuario"];

    private DateTime _desde = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime Desde
    {
        get => _desde;
        set { _desde = value; OnPropertyChanged(); }
    }

    private DateTime _hasta = DateTime.Today;
    public DateTime Hasta
    {
        get => _hasta;
        set { _hasta = value; OnPropertyChanged(); }
    }

    private string _entidad = "Todas";
    public string Entidad
    {
        get => _entidad;
        set { _entidad = value; OnPropertyChanged(); }
    }

    public string TotalRegistrosTexto => $"{Registros.Count} registro(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

    public bool SinRegistros => Registros.Count == 0 && string.IsNullOrWhiteSpace(AvisoApi);

    public ICommand ActualizarCommand { get; }

    public BitacoraViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        ActualizarCommand = new Command(async () => await CargarDesdeApiAsync());
    }

    public async Task CargarDesdeApiAsync()
    {
        var registros = await _api.GetBitacoraAsync(Desde, Hasta, null, Entidad, null);
        if (registros is null)
        {
            AvisoApi = _api.UltimoError ?? "Sin conexión al API.";
            OnPropertyChanged(nameof(SinRegistros));
            return;
        }

        AvisoApi = string.Empty;
        Registros.Clear();
        foreach (var r in registros)
        {
            Registros.Add(new BitacoraItem
            {
                FechaHora = r.FechaHora,
                UsuarioNombre = r.UsuarioNombre,
                Accion = r.Accion,
                Entidad = r.Entidad,
                EntidadId = r.EntidadId,
                Detalle = r.Detalle
            });
        }
        OnPropertyChanged(nameof(TotalRegistrosTexto));
        OnPropertyChanged(nameof(SinRegistros));
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
