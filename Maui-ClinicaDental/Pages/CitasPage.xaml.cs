using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static Maui_ClinicaDental.Models.ApiModelExtensions;

namespace Maui_ClinicaDental.Pages;

public partial class CitasPage : ContentPage
{
    public CitasPage()
    {
        InitializeComponent();
        BindingContext = new CitasViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CitasViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class CitasViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Datos de prueba
    private List<CitaItem> _todas = new()
    {
        new() { Id = 1, FechaHora = DateTime.Today.AddHours(9), DuracionMinutos = 45, PacienteNombre = "María López", Tratamiento = "Limpieza dental", DentistaNombre = "Dra. Ana Pérez", Estado = "Confirmada" },
        new() { Id = 2, FechaHora = DateTime.Today.AddHours(10), DuracionMinutos = 60, PacienteNombre = "Carlos Ruiz", Tratamiento = "Ortodoncia — revisión", DentistaNombre = "Dr. Luis Gómez", Estado = "Programada" },
        new() { Id = 3, FechaHora = DateTime.Today.AddHours(11).AddMinutes(30), DuracionMinutos = 30, PacienteNombre = "Sofía Mendoza", Tratamiento = "Revisión general", DentistaNombre = "Dra. Ana Pérez", Estado = "Confirmada" },
        new() { Id = 4, FechaHora = DateTime.Today.AddDays(1).AddHours(9).AddMinutes(30), DuracionMinutos = 45, PacienteNombre = "María López", Tratamiento = "Limpieza dental", DentistaNombre = "Dra. Ana Pérez", Estado = "Programada" },
    };

    public ObservableCollection<CitaItem> CitasFiltradas { get; } = new();

    public ObservableCollection<string> Estados { get; } = new()
    {
        "Todas", "Programada", "Confirmada", "Completada", "Cancelada", "NoAsistio",
    };

    public string TotalCitasTexto => $"{CitasFiltradas.Count} cita(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }

    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

    private DateTime _fechaFiltro = DateTime.Today;
    public DateTime FechaFiltro
    {
        get => _fechaFiltro;
        set { _fechaFiltro = value; OnPropertyChanged(); _ = CargarDesdeApiAsync(); }
    }

    private string _estadoFiltro = "Todas";
    public string EstadoFiltro
    {
        get => _estadoFiltro;
        set { _estadoFiltro = value; OnPropertyChanged(); _ = CargarDesdeApiAsync(); }
    }

    private bool _mostrarTodasLasFechas;
    public bool MostrarTodasLasFechas
    {
        get => _mostrarTodasLasFechas;
        set { _mostrarTodasLasFechas = value; OnPropertyChanged(); _ = CargarDesdeApiAsync(); }
    }

    public ICommand NuevaCitaCommand { get; }
    public ICommand VerCitaCommand { get; }

    public CitasViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        NuevaCitaCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(NuevaCitaPage)));
        VerCitaCommand = new Command<CitaItem>(async c => await VerCitaAsync(c));
        Filtrar();
    }

    public async Task CargarDesdeApiAsync()
    {
        var citas = await _api.GetCitasAsync((MostrarTodasLasFechas ? null : FechaFiltro), EstadoFiltro);
        if (citas is null)
        {
            AvisoApi = "Sin conexión al API, mostrando datos de ejemplo.";
            Filtrar();
            return;
        }

        AvisoApi = string.Empty;
        // El servidor ya filtró; guardamos el resultado como lista base
        // y el filtro local solo reordena / vuelve a aplicar por si acaso.
        _todas = citas.Select(c => c.ToCitaItem()).ToList();
        CitasFiltradas.Clear();
        foreach (var c in _todas.OrderBy(c => c.FechaHora)) CitasFiltradas.Add(c);
        OnPropertyChanged(nameof(TotalCitasTexto));
    }

    private void Filtrar()
    {
        var filtradas = _todas
            .Where(c => MostrarTodasLasFechas || c.FechaHora.Date == FechaFiltro.Date)
            .Where(c => EstadoFiltro == "Todas" || c.Estado == EstadoFiltro)
            .OrderBy(c => c.FechaHora)
            .ToList();

        CitasFiltradas.Clear();
        foreach (var c in filtradas) CitasFiltradas.Add(c);
        OnPropertyChanged(nameof(TotalCitasTexto));
    }

    private async Task VerCitaAsync(CitaItem? cita)
    {
        if (cita is null) return;

            var opciones = new List<string>
            {
                "Ver detalle",
                "Cambiar estado",
                "Registrar pago",
                "Ver pagos",
                "Editar",
            };
            // 2026-11-20 (login): eliminar es solo para Admin; a un
            // recepcionista ni siquiera se le muestra la opción (el
            // servidor también lo bloquea con 403).
            if (_api.EsAdmin) opciones.Add("Eliminar");

            var accion = await Shell.Current.DisplayActionSheetAsync(
                $"{cita.PacienteNombre} — {cita.FechaHora:dddd dd/MM HH:mm}",
                "Cerrar",
                null,
                opciones.ToArray());

        switch (accion)
        {
            case "Cambiar estado":
                await CambiarEstadoAsync(cita);
                break;
            case "Registrar pago":
                await RegistrarPagoAsync(cita);
                break;
            case "Ver pagos":
                await Shell.Current.GoToAsync(
                    $"{nameof(PagosPage)}?citaId={cita.Id}" +
                    $"&pacienteId={cita.PacienteId}" +
                    $"&pacienteNombre={Uri.EscapeDataString(cita.PacienteNombre)}");
                break;
            case "Ver detalle":
                await MostrarDetalleAsync(cita);
                break;
            case "Editar":
                // NuevaCitaPage en modo edición: precarga el formulario
                // con los datos de esta cita.
                await Shell.Current.GoToAsync(
                    $"{nameof(NuevaCitaPage)}?citaId={cita.Id}");
                break;
            case "Eliminar":
                await EliminarCitaAsync(cita);
                break;
        }
    }

    // Eliminar es un borrado real en el servidor, con confirmación.
    // Si la cita ya tiene pagos, el servidor devuelve 409 y el
    // mensaje explica que hay que cancelarla en vez de borrarla.
    private async Task EliminarCitaAsync(CitaItem cita)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar cita",
            $"¿Eliminar la cita de {cita.PacienteNombre} del {cita.FechaHora:dd/MM/yyyy HH:mm}?",
            "Eliminar", "Cancelar");
        if (!confirmar) return;

        var (ok, error) = await _api.EliminarCitaAsync(cita.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync(
                "No se pudo eliminar", error ?? "El servidor no eliminó la cita.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
        await Shell.Current.DisplayAlertAsync(
            "Cita eliminada", $"La cita de {cita.PacienteNombre} fue eliminada.", "OK");
    }

    private static Task MostrarDetalleAsync(CitaItem cita)
    {
        var pagina = Shell.Current;
        return pagina.DisplayAlertAsync(
            $"{cita.PacienteNombre} — {cita.FechaHora:dddd dd/MM HH:mm}",
            $"{cita.Tratamiento} con {cita.DentistaNombre}\nDuración: {cita.DuracionMinutos} min\nEstado: {cita.Estado}",
            "OK");
    }

    private async Task CambiarEstadoAsync(CitaItem cita)
    {
        var nuevoEstado = await Shell.Current.DisplayActionSheetAsync(
            "Cambiar estado",
            "Cancelar",
            null,
            "Programada", "Confirmada", "Completada", "Cancelada", "NoAsistio");

        if (string.IsNullOrWhiteSpace(nuevoEstado) || nuevoEstado == "Cancelar" || nuevoEstado == cita.Estado)
            return;

        var (ok, error) = await _api.CambiarEstadoCitaAsync(cita.Id, nuevoEstado);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo cambiar el estado", error ?? "El servidor no actualizó la cita.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
        await Shell.Current.DisplayAlertAsync("Estado actualizado", $"La cita de {cita.PacienteNombre} ahora está {nuevoEstado}.", "OK");
    }

    private async Task RegistrarPagoAsync(CitaItem cita)
    {
        var montoTexto = await Shell.Current.DisplayPromptAsync(
            "Registrar pago",
            $"Monto a cobrar por {cita.Tratamiento} ({cita.PacienteNombre}):",
            "Continuar", "Cancelar",
            placeholder: "Ej.: 800",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(montoTexto))
            return;

        if (!decimal.TryParse(montoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) &&
            !decimal.TryParse(montoTexto, NumberStyles.Number, CultureInfo.CurrentCulture, out monto))
        {
            await Shell.Current.DisplayAlertAsync("Monto no válido", "Escribe el monto solo con números, por ejemplo 800 o 800.50.", "OK");
            return;
        }

        if (monto <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Monto no válido", "El monto debe ser mayor que 0.", "OK");
            return;
        }

        var metodo = await Shell.Current.DisplayActionSheetAsync(
            "Método de pago",
            "Cancelar",
            null,
            "Efectivo", "Tarjeta", "Transferencia", "Otro");

        if (string.IsNullOrWhiteSpace(metodo) || metodo == "Cancelar")
            return;

        var (ok, error) = await _api.RegistrarPagoAsync(new CrearPagoRequest(cita.Id, monto, metodo));
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo registrar el pago", error ?? "El servidor no guardó el pago.", "OK");
            return;
        }

        var pagos = await _api.GetPagosAsync(cita.Id);
        var totalPagado = pagos?.Sum(p => p.Monto) ?? monto;
        await Shell.Current.DisplayAlertAsync(
            "Pago registrado",
            $"${monto:0.00} en {metodo} para {cita.PacienteNombre}.\nTotal pagado en esta cita: ${totalPagado:0.00}.\nRecuerda cambiar el estado a Completada si la cita ya terminó.",
            "OK");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
