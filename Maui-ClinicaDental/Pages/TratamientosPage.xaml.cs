using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// TratamientosPage — administrar el catálogo de tratamientos.
// Crear y editar abren NuevoTratamientoPage, un formulario con todos
// los campos a la vez (2026-11-19: antes eran preguntas en pantalla
// una por una, y si se cancelaba alguna no se guardaba nada, así que
// parecía que no actualizaba). Eliminar es una baja lógica en el
// servidor (Activo = false): el tratamiento deja de aparecer en el
// catálogo, pero el precio cobrado en las citas anteriores queda
// guardado en CitaTratamientos y no se rompe.
public partial class TratamientosPage : ContentPage
{
    public TratamientosPage()
    {
        InitializeComponent();
        BindingContext = new TratamientosViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TratamientosViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class TratamientosViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Datos de ejemplo, iguales a ConsultorioDental.sql. Solo se usan
    // si el API no responde.
    private List<TratamientoDto> _todos =
    [
        new(1, "Limpieza dental", "Limpieza completa con pulido", 800.00m, 45),
        new(2, "Revisión general", "Revisión y diagnóstico", 500.00m, 30),
        new(3, "Ortodoncia — revisión", "Revisión de ortodoncia", 1200.00m, 60),
    ];

    public ObservableCollection<TratamientoDto> Tratamientos { get; } = [];

    public string TotalTratamientosTexto => $"{Tratamientos.Count} tratamiento(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);
    // 2026-11-25 (loading): true mientras se valida el acceso al API. La lista se oculta mientras carga para no mostrar el "No hay registros" antes de tiempo.
    private bool _estaCargando;
    public bool EstaCargando
    {
        get => _estaCargando;
        private set { _estaCargando = value; OnPropertyChanged(); OnPropertyChanged(nameof(ListaVisible)); }
    }
    public bool ListaVisible => !EstaCargando;

    public ICommand NuevoTratamientoCommand { get; }
    public ICommand VerTratamientoCommand { get; }

    public TratamientosViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        NuevoTratamientoCommand = new Command(async () => await NuevoTratamientoAsync());
        VerTratamientoCommand = new Command<TratamientoDto>(async t => await VerTratamientoAsync(t));
        Refrescar();
    }

    public async Task CargarDesdeApiAsync()
    {
        using var _ = CargandoScope.Activar(v => EstaCargando = v);
        var tratamientos = await _api.GetTratamientosAsync();
        if (tratamientos is null)
        {
            AvisoApi = "Sin conexión al API, mostrando datos de ejemplo.";
            Refrescar();
            return;
        }

        AvisoApi = string.Empty;
        _todos = tratamientos;
        Refrescar();
    }

    private void Refrescar()
    {
        Tratamientos.Clear();
        foreach (var t in _todos) Tratamientos.Add(t);
        OnPropertyChanged(nameof(TotalTratamientosTexto));
    }

    private async Task VerTratamientoAsync(TratamientoDto? tratamiento)
    {
        if (tratamiento is null) return;

        var accion = await Shell.Current.DisplayActionSheetAsync(
            tratamiento.Nombre,
            "Cerrar",
            null,
            "Editar",
            "Eliminar");

        switch (accion)
        {
            case "Editar":
                await EditarTratamientoAsync(tratamiento);
                break;
            case "Eliminar":
                await EliminarTratamientoAsync(tratamiento);
                break;
        }
    }

    private Task NuevoTratamientoAsync()
        // NuevoTratamientoPage sin parámetro: formulario vacío para crear.
        => Shell.Current.GoToAsync(nameof(NuevoTratamientoPage));

    private Task EditarTratamientoAsync(TratamientoDto tratamiento)
        // NuevoTratamientoPage en modo edición: precarga el formulario
        // con los datos de este tratamiento, con todos los campos a la
        // vez en pantalla. La lista se recarga sola al volver, en
        // OnAppearing.
        => Shell.Current.GoToAsync(
            $"{nameof(NuevoTratamientoPage)}?tratamientoId={tratamiento.TratamientoId}");

    private async Task EliminarTratamientoAsync(TratamientoDto tratamiento)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar tratamiento",
            $"¿Eliminar {tratamiento.Nombre}? Dejará de aparecer en el catálogo, pero el precio de las citas ya cobradas no cambia.",
            "Eliminar", "Cancelar");
        if (!confirmar) return;

        var (ok, error) = await _api.EliminarTratamientoAsync(tratamiento.TratamientoId);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo eliminar", error ?? "El servidor no eliminó el tratamiento.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
        await Shell.Current.DisplayAlertAsync("Tratamiento eliminado", $"{tratamiento.Nombre} ya no aparece en el catálogo.", "OK");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
