using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental;

[QueryProperty(nameof(Buscar), "buscar")]
public partial class PacientesPage : ContentPage
{
    public PacientesPage()
    {
        InitializeComponent();
        BindingContext = new PacientesViewModel();
    }

    // Lo que MainPage pasa en la ruta: PacientesPage?buscar=maría
    public string Buscar
    {
        set
        {
            if (BindingContext is PacientesViewModel vm && !string.IsNullOrWhiteSpace(value))
                vm.TextoBusqueda = Uri.UnescapeDataString(value);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PacientesViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class PacientesViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Datos de ejemplo, iguales a ConsultorioDental.sql. Solo se usan
    // si el API no responde.
    private List<PacienteItem> _todos =
    [
        new() { Id = 1, Nombre = "María", Apellido = "López", FechaNacimiento = new DateTime(1990, 5, 12), Telefono = "555-1001", Email = "maria.lopez@test.com" },
        new() { Id = 2, Nombre = "Carlos", Apellido = "Ruiz", FechaNacimiento = new DateTime(1985, 9, 3), Telefono = "555-1002", Email = "carlos.ruiz@test.com" },
        new() { Id = 3, Nombre = "Sofía", Apellido = "Mendoza", FechaNacimiento = new DateTime(1998, 2, 20), Telefono = "555-1003", Email = "sofia.mendoza@test.com" },
    ];

    public ObservableCollection<PacienteItem> PacientesFiltrados { get; } = [];

    public string TotalPacientesTexto => $"{PacientesFiltrados.Count} paciente(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { _textoBusqueda = value; OnPropertyChanged(); Filtrar(); }
    }

    public ICommand NuevoPacienteCommand { get; }
    public ICommand VerPacienteCommand { get; }

    public PacientesViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        NuevoPacienteCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(NuevoPacientePage)));
        VerPacienteCommand = new Command<PacienteItem>(async p => await VerPacienteAsync(p));
        Filtrar();
    }

    public async Task CargarDesdeApiAsync()
    {
        var pacientes = await _api.GetPacientesAsync();
        if (pacientes is null)
        {
            AvisoApi = "Sin conexión al API, mostrando datos de ejemplo.";
            Filtrar();
            return;
        }

        AvisoApi = string.Empty;
        _todos = pacientes.Select(p => p.ToPacienteItem()).ToList();
        Filtrar();
    }

    private void Filtrar()
    {
        var texto = TextoBusqueda.Trim().ToLowerInvariant();
        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _todos
            : _todos.Where(p =>
                p.NombreCompleto.ToLowerInvariant().Contains(texto) ||
                (p.Telefono ?? "").Contains(texto)).ToList();

        PacientesFiltrados.Clear();
        foreach (var p in filtrados) PacientesFiltrados.Add(p);
        OnPropertyChanged(nameof(TotalPacientesTexto));
    }

    private async Task VerPacienteAsync(PacienteItem? paciente)
    {
        if (paciente is null) return;

        var accion = await Shell.Current.DisplayActionSheetAsync(
            paciente.NombreCompleto,
            "Cerrar",
            null,
            "Ver pagos",
            "Nueva cita",
            "Editar",
            "Eliminar");

        switch (accion)
        {
            case "Ver pagos":
                await Shell.Current.GoToAsync(
                    $"{nameof(PagosPage)}?pacienteId={paciente.Id}" +
                    $"&pacienteNombre={Uri.EscapeDataString(paciente.NombreCompleto)}");
                break;
            case "Nueva cita":
                await Shell.Current.GoToAsync(nameof(NuevaCitaPage));
                break;
            case "Editar":
                // NuevoPacientePage en modo edición: precarga el
                // formulario con los datos de este paciente.
                await Shell.Current.GoToAsync(
                    $"{nameof(NuevoPacientePage)}?pacienteId={paciente.Id}");
                break;
            case "Eliminar":
                await EliminarPacienteAsync(paciente);
                break;
        }
    }

    // Eliminar es una baja lógica en el servidor (Activo = false):
    // el paciente deja de aparecer en la lista, pero sus citas y
    // pagos quedan intactos en el historial. Se pide confirmación
    // porque desde la app no hay forma de reactivarlo.
    private async Task EliminarPacienteAsync(PacienteItem paciente)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar paciente",
            $"¿Eliminar a {paciente.NombreCompleto}? Dejará de aparecer en la lista, pero sus citas y pagos se conservan.",
            "Eliminar", "Cancelar");
        if (!confirmar) return;

        var (ok, error) = await _api.EliminarPacienteAsync(paciente.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync(
                "No se pudo eliminar", error ?? "El servidor no eliminó el paciente.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
        await Shell.Current.DisplayAlertAsync(
            "Paciente eliminado", $"{paciente.NombreCompleto} ya no aparece en la lista.", "OK");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}