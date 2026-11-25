using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// ReportesPage — totales por día/mes, tratamientos más usados y
// citas por dentista (2026-11-19). El período por defecto es el mes
// en curso. Las 4 llamadas se hacen en paralelo y, si el API no
// responde, se muestra el aviso amarillo y las listas quedan vacías:
// para reportes no hay datos de ejemplo, porque unos números
// inventados se confundirían con los reales.
public partial class ReportesPage : ContentPage
{
    public ReportesPage()
    {
        InitializeComponent();
        BindingContext = new ReportesViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReportesViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class ReportesViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    private DateTime _desde = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime Desde
    {
        get => _desde;
        set { _desde = value.Date; OnPropertyChanged(); }
    }

    private DateTime _hasta = DateTime.Today;
    public DateTime Hasta
    {
        get => _hasta;
        set { _hasta = value.Date; OnPropertyChanged(); }
    }

    private ResumenReporteDto? _resumen;
    public ResumenReporteDto? Resumen
    {
        get => _resumen;
        private set
        {
            _resumen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TieneResumen));
            OnPropertyChanged(nameof(TotalCobradoTexto));
            OnPropertyChanged(nameof(CantidadPagosTexto));
            OnPropertyChanged(nameof(TotalCitasTexto));
            OnPropertyChanged(nameof(PromedioTexto));
            OnPropertyChanged(nameof(CitasPorEstadoTexto));
        }
    }

    public bool TieneResumen => Resumen is not null;
    public string TotalCobradoTexto => Resumen is null ? "$0.00" : $"${Resumen.TotalCobrado:0.00}";
    public string CantidadPagosTexto => Resumen?.CantidadPagos.ToString() ?? "0";
    public string TotalCitasTexto => Resumen?.TotalCitas.ToString() ?? "0";
    public string PromedioTexto => Resumen is null ? "$0.00" : $"${Resumen.PromedioPorPago:0.00}";

    public string CitasPorEstadoTexto => Resumen is null || Resumen.CitasPorEstado.Count == 0
        ? string.Empty
        : "Citas por estado: " + string.Join(" · ",
            Resumen.CitasPorEstado.Select(e => $"{e.Estado}: {e.Cantidad}"));

    public ObservableCollection<CobroPorDiaDto> CobrosPorDia { get; } = new();
    public ObservableCollection<TratamientoUsoDto> TratamientosMasUsados { get; } = new();
    public ObservableCollection<CitasPorDentistaDto> CitasPorDentista { get; } = new();

    public bool SinCobros => CobrosPorDia.All(c => c.CantidadPagos == 0);
    public bool SinTratamientos => TratamientosMasUsados.Count == 0;
    public bool SinCitasPorDentista => CitasPorDentista.Count == 0;

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);
    // 2026-11-25 (loading): true mientras se valida el acceso al API.
    private bool _estaCargando;
    public bool EstaCargando
    {
        get => _estaCargando;
        private set { _estaCargando = value; OnPropertyChanged(); }
    }

    public ICommand ActualizarCommand { get; }

    public ReportesViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        ActualizarCommand = new Command(async () => await CargarDesdeApiAsync());
    }

    public async Task CargarDesdeApiAsync()
    {
        using var _ = CargandoScope.Activar(v => EstaCargando = v);
        if (Desde > Hasta)
        {
            AvisoApi = "La fecha Desde no puede ser mayor que Hasta.";
            return;
        }

        var resumenTask = _api.GetResumenReportesAsync(Desde, Hasta);
        var cobrosTask = _api.GetCobrosPorDiaAsync(Desde, Hasta);
        var tratamientosTask = _api.GetTratamientosMasUsadosAsync(Desde, Hasta);
        var dentistasTask = _api.GetCitasPorDentistaAsync(Desde, Hasta);
        await Task.WhenAll(resumenTask, cobrosTask, tratamientosTask, dentistasTask);

        var resumen = await resumenTask;
        var cobros = await cobrosTask;
        var tratamientos = await tratamientosTask;
        var dentistas = await dentistasTask;

        if (resumen is null || cobros is null || tratamientos is null || dentistas is null)
        {
            AvisoApi = "Sin conexión al API, no se pudieron cargar los reportes. " + (_api.UltimoError ?? "");
            return;
        }

        AvisoApi = string.Empty;
        Resumen = resumen;

        CobrosPorDia.Clear();
        foreach (var dia in cobros)
            CobrosPorDia.Add(dia);
        OnPropertyChanged(nameof(SinCobros));

        TratamientosMasUsados.Clear();
        foreach (var t in tratamientos)
            TratamientosMasUsados.Add(t);
        OnPropertyChanged(nameof(SinTratamientos));

        CitasPorDentista.Clear();
        foreach (var d in dentistas)
            CitasPorDentista.Add(d);
        OnPropertyChanged(nameof(SinCitasPorDentista));
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
