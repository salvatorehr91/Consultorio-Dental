using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// DentistasPage — administrar el catálogo de dentistas.
// Crear y editar abren NuevoDentistaPage, un formulario con todos
// los campos a la vez (2026-11-19: antes eran preguntas en pantalla
// una por una, y para cambiar un solo campo había que pasar por las
// 5). Eliminar es una baja lógica en el servidor (Activo = false):
// el dentista deja de aparecer, pero sus citas quedan intactas en
// el historial.
public partial class DentistasPage : ContentPage
{
    public DentistasPage()
    {
        InitializeComponent();
        BindingContext = new DentistasViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DentistasViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class DentistasViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Datos de ejemplo, iguales a ConsultorioDental.sql. Solo se usan
    // si el API no responde.
    private List<DentistaItem> _todos =
    [
        new() { Id = 1, Nombre = "Ana", Apellido = "Pérez", Especialidad = "General", Telefono = "555-0101", Email = "ana.perez@test.com" },
        new() { Id = 2, Nombre = "Luis", Apellido = "Gómez", Especialidad = "Ortodoncia", Telefono = "555-0102", Email = "luis.gomez@test.com" },
    ];

    public ObservableCollection<DentistaItem> Dentistas { get; } = [];

    public string TotalDentistasTexto => $"{Dentistas.Count} dentista(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

    public ICommand NuevoDentistaCommand { get; }
    public ICommand VerDentistaCommand { get; }

    public DentistasViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        NuevoDentistaCommand = new Command(async () => await NuevoDentistaAsync());
        VerDentistaCommand = new Command<DentistaItem>(async d => await VerDentistaAsync(d));
        Refrescar();
    }

    public async Task CargarDesdeApiAsync()
    {
        var dentistas = await _api.GetDentistasAsync();
        if (dentistas is null)
        {
            AvisoApi = "Sin conexión al API, mostrando datos de ejemplo.";
            Refrescar();
            return;
        }

        AvisoApi = string.Empty;
        _todos = dentistas.Select(d => d.ToDentistaItem()).ToList();
        Refrescar();
    }

    private void Refrescar()
    {
        Dentistas.Clear();
        foreach (var d in _todos) Dentistas.Add(d);
        OnPropertyChanged(nameof(TotalDentistasTexto));
    }

    private async Task VerDentistaAsync(DentistaItem? dentista)
    {
        if (dentista is null) return;

        var accion = await Shell.Current.DisplayActionSheetAsync(
            dentista.NombreCompleto,
            "Cerrar",
            null,
            "Editar",
            "Eliminar");

        switch (accion)
        {
            case "Editar":
                await EditarDentistaAsync(dentista);
                break;
            case "Eliminar":
                await EliminarDentistaAsync(dentista);
                break;
        }
    }

    private Task NuevoDentistaAsync()
        // NuevoDentistaPage sin parámetro: formulario vacío para crear.
        => Shell.Current.GoToAsync(nameof(NuevoDentistaPage));

    private Task EditarDentistaAsync(DentistaItem dentista)
        // NuevoDentistaPage en modo edición: precarga el formulario
        // con los datos de este dentista, con todos los campos a la
        // vez en pantalla. La lista se recarga sola al volver, en
        // OnAppearing.
        => Shell.Current.GoToAsync(
            $"{nameof(NuevoDentistaPage)}?dentistaId={dentista.Id}");

    private async Task EliminarDentistaAsync(DentistaItem dentista)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar dentista",
            $"¿Eliminar a {dentista.NombreCompleto}? Dejará de aparecer en la lista y ya no se le podrán crear citas, pero sus citas anteriores se conservan.",
            "Eliminar", "Cancelar");
        if (!confirmar) return;

        var (ok, error) = await _api.EliminarDentistaAsync(dentista.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo eliminar", error ?? "El servidor no eliminó el dentista.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
        await Shell.Current.DisplayAlertAsync("Dentista eliminado", $"{dentista.NombreCompleto} ya no aparece en la lista.", "OK");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
