// PagosPage.xaml.cs — Historial de pagos y comprobante (.NET MAUI)
// Carga GET /api/pagos con filtros (citaId opcional por QueryProperty,
// desde/hasta por FechaPago y método). Muestra el total de pagos con
// Estado "Pagado". Al tocar un pago, trae su comprobante de
// GET /api/pagos/{id}/comprobante y lo muestra abajo.
//
// Imprimir: MAUI no tiene una API de impresión multiplataforma, así
// que el comprobante se genera como HTML y:
//  - "Abrir para imprimir" lo abre en el navegador -> Ctrl+P.
//  - "Compartir" lo manda por el menú del sistema (Share), que en
//    Windows / Android / iOS ofrece Imprimir o guardar como PDF.
// El archivo se escribe en FileSystem.CacheDirectory y no se guarda
// en la app: es solo el comprobante del momento.

using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Maui_ClinicaDental;

[QueryProperty(nameof(CitaIdQuery), "citaId")]
[QueryProperty(nameof(PacienteIdQuery), "pacienteId")]
[QueryProperty(nameof(PacienteNombreQuery), "pacienteNombre")]
public partial class PagosPage : ContentPage
{
    public PagosPage()
    {
        InitializeComponent();
        ViewModel = new PagosViewModel();
        BindingContext = ViewModel;
    }

    public PagosViewModel ViewModel { get; }

    public string? CitaIdQuery
    {
        set
        {
            if (int.TryParse(value, out var id))
                ViewModel.CitaId = id;
        }
    }

    public string? PacienteIdQuery
    {
        set
        {
            if (int.TryParse(value, out var id))
                ViewModel.PacienteId = id;
        }
    }

    // Nombre opcional, por si se navega solo con el nombre o para
    // mostrarlo antes de que cargue el catálogo de pacientes.
    public string? PacienteNombreQuery
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ViewModel.PacienteNombreQuery = Uri.UnescapeDataString(value);
                ViewModel.BloquearFiltroPaciente();
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.CargarAsync();
    }

    public class PagosViewModel : BindableObject
    {
        private readonly ApiService _api = new();
        private List<PagoDto> _todos = [];
        private PagoDto? _seleccionado;
        private ComprobanteDto? _comprobante;

        public int? CitaId { get; set; }
        public int? PacienteId { get; set; }
        public string? PacienteNombreQuery { get; set; }
        public ObservableCollection<PagoDto> PagosFiltrados { get; } = new();
        public ObservableCollection<PacienteDto> Pacientes { get; } = new();

        // Bloqueo del filtro de paciente: true cuando se llegó a la
        // página con ?pacienteId= / ?pacienteNombre= (desde Pacientes).
        // El Picker queda deshabilitado y el nombre se muestra arriba.
        private bool _pacienteBloqueado;
        public bool PacienteBloqueado
        {
            get => _pacienteBloqueado;
            private set
            {
                _pacienteBloqueado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FiltroPacienteHabilitado));
                OnPropertyChanged(nameof(PacienteSeleccionadoTexto));
            }
        }
        public bool FiltroPacienteHabilitado => !PacienteBloqueado;

        public string PacienteSeleccionadoTexto =>
            PacienteBloqueado
                ? $"Historial de: {PacienteFiltro?.NombreCompleto ?? PacienteNombreQuery ?? "—"} — filtro bloqueado"
                : string.Empty;

        public void BloquearFiltroPaciente() => PacienteBloqueado = true;

        private DateTime _fechaDesde = DateTime.Today.AddDays(-30);
        public DateTime FechaDesde
        {
            get => _fechaDesde;
            set
            {
                _fechaDesde = value.Date;
                if (_fechaDesde > FechaHasta)
                    FechaHasta = _fechaDesde;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        private DateTime _fechaHasta = DateTime.Today;
        public DateTime FechaHasta
        {
            get => _fechaHasta;
            set
            {
                _fechaHasta = value.Date;
                if (_fechaHasta < FechaDesde)
                    FechaDesde = _fechaHasta;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        private PacienteDto? _pacienteFiltro;
        public PacienteDto? PacienteFiltro
        {
            get => _pacienteFiltro;
            set
            {
                _pacienteFiltro = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PacienteSeleccionadoTexto));
                AplicarFiltros();
            }
        }

        public ObservableCollection<string> Metodos { get; } = new()
            { "Todos", "Efectivo", "Tarjeta", "Transferencia", "Otro" };

        private string _metodoFiltro = "Todos";
        public string MetodoFiltro
        {
            get => _metodoFiltro;
            set { _metodoFiltro = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        private string _totalTexto = "0 pagos";
        public string TotalTexto
        {
            get => _totalTexto;
            set { _totalTexto = value; OnPropertyChanged(); }
        }

        private string _avisoApi = string.Empty;
        public string AvisoApi
        {
            get => _avisoApi;
            set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
        }
        public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

        public bool TieneComprobante => _comprobante is not null;

        public string ComprobanteTitulo =>
            _comprobante is null ? string.Empty
            : $"Comprobante {_comprobante.Folio} — {_comprobante.Monto.ToString("C", CultureInfo.GetCultureInfo("es-MX"))}";

        // Datos del consultorio: vienen del API (appsettings.json).
        // Los "TU_..." son placeholders por llenar, no se muestran.
        private static string DatoConsultorio(string? valor) =>
            string.IsNullOrWhiteSpace(valor) || valor.StartsWith("TU_")
                ? string.Empty
                : valor;

        public string ComprobanteDetalle
        {
            get
            {
                if (_comprobante is null)
                    return string.Empty;
                var c = _comprobante;
                var lineas = new List<string>();
                var nombre = DatoConsultorio(c.ConsultorioNombre);
                if (nombre.Length > 0) lineas.Add(nombre);
                var direccion = DatoConsultorio(c.ConsultorioDireccion);
                if (direccion.Length > 0) lineas.Add(direccion);
                var contacto = string.Join(" · ", new[]
                    {
                        string.IsNullOrWhiteSpace(DatoConsultorio(c.ConsultorioTelefono)) ? null : $"Tel: {DatoConsultorio(c.ConsultorioTelefono)}",
                        DatoConsultorio(c.ConsultorioEmail),
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (contacto.Length > 0) lineas.Add(contacto);
                lineas.Add($"Paciente: {c.PacienteNombre}");
                lineas.Add($"Tratamiento: {c.Tratamiento}");
                lineas.Add($"Cita: {c.FechaCita:dd/MM/yyyy HH:mm} con {c.DentistaNombre}");
                lineas.Add($"Método: {c.MetodoPago} · Estado: {c.EstadoPago}");
                lineas.Add($"Pagado el {c.FechaPago:dd/MM/yyyy HH:mm}");
                return string.Join("\n", lineas);
            }
        }

        public Command<PagoDto> VerComprobanteCommand { get; }
        public Command CompartirComprobanteCommand { get; }
        public Command ImprimirComprobanteCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command CerrarComprobanteCommand { get; }

        public PagosViewModel()
        {
            VerComprobanteCommand = new Command<PagoDto>(async p => await VerComprobanteAsync(p));
            CompartirComprobanteCommand = new Command(async () => await CompartirAsync());
            ImprimirComprobanteCommand = new Command(async () => await ImprimirAsync());
            LimpiarFiltrosCommand = new Command(LimpiarFiltros);
            CerrarComprobanteCommand = new Command(CerrarComprobante);
            Pacientes.Add(new PacienteDto(0, "Todos", string.Empty, null, null, null, null));
            PacienteFiltro = Pacientes[0];
        }

        private void LimpiarFiltros()
        {
            // Los setters ya llaman a AplicarFiltros; el último deja la lista lista.
            // Si el paciente llegó bloqueado por QueryProperty, no se toca.
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            if (!PacienteBloqueado)
                PacienteFiltro = Pacientes.FirstOrDefault(p => p.PacienteId == 0) ?? Pacientes[0];
            MetodoFiltro = "Todos";
        }

        public async Task CargarAsync()
        {
            // Una sola carga: el servidor solo pre-filtra por cita/paciente
            // (con su rango de fecha ampliado 1 día por el tema UTC).
            // Fecha, paciente y método se aplican en local, en AplicarFiltros.
            var pagos = await _api.GetPagosAsync(CitaId, null, null, null, PacienteId);
            AvisoApi = _api.UltimoError is null
                ? string.Empty
                : "Sin conexión con el API. Revisa que esté corriendo.";
            _todos = pagos ?? new List<PagoDto>();

            // Catálogo de pacientes para el filtro (solo si aún no está).
            if (Pacientes.Count <= 1)
            {
                var pacientes = await _api.GetPacientesAsync();
                if (pacientes is not null)
                {
                    foreach (var p in pacientes)
                        Pacientes.Add(p);
                }
            }

            // Si se llegó con ?pacienteId= / ?pacienteNombre=, seleccionar
            // ese paciente y dejarlo bloqueado. Si el id no está en el
            // catálogo (p. ej. API sin ese paciente), se agrega con el
            // nombre que llegó por query, para que se vea seleccionado.
            if (PacienteBloqueado)
            {
                PacienteDto? elegido = null;
                if (PacienteId.HasValue)
                    elegido = Pacientes.FirstOrDefault(p => p.PacienteId == PacienteId.Value);
                if (elegido is null && !string.IsNullOrWhiteSpace(PacienteNombreQuery))
                    elegido = Pacientes.FirstOrDefault(p =>
                        string.Equals(p.NombreCompleto, PacienteNombreQuery, StringComparison.OrdinalIgnoreCase));
                if (elegido is null && PacienteId.HasValue && !string.IsNullOrWhiteSpace(PacienteNombreQuery))
                {
                    var partes = PacienteNombreQuery.Split(' ', 2);
                    elegido = new PacienteDto(PacienteId.Value,
                        partes[0], partes.Length > 1 ? partes[1] : string.Empty,
                        null, null, null, null);
                    Pacientes.Add(elegido);
                }
                if (elegido is not null)
                    PacienteFiltro = elegido;
            }

            AplicarFiltros();
        }

        // Oculta el comprobante: pone TieneComprobante en false y la
        // fila del Grid se colapsa, así la lista recupera el espacio.
        // También se llama al cambiar los filtros, para no dejar un
        // comprobante de un pago que ya no está en la lista.
        private void CerrarComprobante()
        {
            _comprobante = null;
            _seleccionado = null;
            OnPropertyChanged(nameof(TieneComprobante));
            OnPropertyChanged(nameof(ComprobanteTitulo));
            OnPropertyChanged(nameof(ComprobanteDetalle));
        }

        private void AplicarFiltros()
        {
            CerrarComprobante();
            var desde = FechaDesde.Date;
            var hasta = FechaHasta.Date;
            var pacienteId = PacienteFiltro?.PacienteId ?? 0;

            var filtrados = _todos.Where(p =>
            {
                // Fecha en hora local del consultorio, no en UTC.
                var fechaLocal = p.FechaPago.Kind == DateTimeKind.Utc
                    ? p.FechaPago.ToLocalTime().Date
                    : p.FechaPago.Date;
                if (fechaLocal < desde || fechaLocal > hasta)
                    return false;
                if (!string.IsNullOrWhiteSpace(MetodoFiltro) && MetodoFiltro != "Todos"
                    && p.MetodoPago != MetodoFiltro)
                    return false;
                if (pacienteId != 0)
                {
                    // PacienteId 0 = API vieja sin ese campo: respaldo por nombre.
                    if (p.PacienteId != 0)
                        return p.PacienteId == pacienteId;
                    return p.PacienteNombre == (PacienteFiltro?.NombreCompleto ?? string.Empty);
                }
                return true;
            }).ToList();

            PagosFiltrados.Clear();
            foreach (var p in filtrados)
                PagosFiltrados.Add(p);

            var pagados = filtrados.Where(p => p.Estado == "Pagado").Sum(p => p.Monto);
            var total = pagados.ToString("C", CultureInfo.GetCultureInfo("es-MX"));
            TotalTexto = CitaId.HasValue
                ? $"{filtrados.Count} pago(s) de esta cita — Total pagado: {total}"
                : pacienteId != 0
                    ? $"{filtrados.Count} pago(s) de {PacienteFiltro?.NombreCompleto} — Total pagado: {total}"
                    : $"{filtrados.Count} pago(s) — Total recaudado (Pagado): {total}";
        }

        private async Task VerComprobanteAsync(PagoDto pago)
        {
            // Tocar de nuevo el mismo pago lo oculta.
            if (_seleccionado?.PagoId == pago.PagoId && _comprobante is not null)
            {
                CerrarComprobante();
                return;
            }

            _seleccionado = pago;
            _comprobante = await _api.GetComprobanteAsync(pago.PagoId);
            if (_comprobante is null)
            {
                await Shell.Current.DisplayAlertAsync("Comprobante",
                    _api.UltimoError ?? "No se pudo cargar el comprobante.", "OK");
                return;
            }
            OnPropertyChanged(nameof(TieneComprobante));
            OnPropertyChanged(nameof(ComprobanteTitulo));
            OnPropertyChanged(nameof(ComprobanteDetalle));
        }

        private string GenerarHtmlComprobante()
        {
            if (_comprobante is null)
                return string.Empty;

            var c = _comprobante;
            var cultura = CultureInfo.GetCultureInfo("es-MX");
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>Comprobante ").Append(c.Folio).Append("</title>");
            sb.Append("<style>body{font-family:Arial,sans-serif;max-width:560px;margin:40px auto;color:#1A2B3C}" +
                      "h1{color:#0F3556;font-size:22px}h2{color:#0E7C7B;font-size:18px}" +
                      "table{width:100%;border-collapse:collapse}td{padding:8px 0;border-bottom:1px solid #DDD}" +
                      "td:first-child{color:#5A6B7A}.total{font-size:20px;font-weight:bold;color:#0F3556}" +
                      "@media print{body{margin:0}}</style></head><body>");
            // Encabezado con los datos del consultorio (del API).
            // Solo se muestran los datos reales; los "TU_..." no salen en el recibo.
            sb.Append("<h1>").Append(DatoConsultorio(c.ConsultorioNombre)).Append("</h1>");
            var direccionHtml = DatoConsultorio(c.ConsultorioDireccion);
            var telefonoHtml = DatoConsultorio(c.ConsultorioTelefono);
            var emailHtml = DatoConsultorio(c.ConsultorioEmail);
            var contactoHtml = string.Join(" · ", new[] { telefonoHtml, emailHtml }.Where(s => s.Length > 0));
            if (direccionHtml.Length > 0)
                sb.Append("<p>").Append(direccionHtml).Append("</p>");
            if (contactoHtml.Length > 0)
                sb.Append("<p>").Append(contactoHtml).Append("</p>");
            sb.Append("<h2>Comprobante de pago — ").Append(c.Folio).Append("</h2>");
            sb.Append("<table>");
            sb.Append("<tr><td>Paciente</td><td>").Append(c.PacienteNombre).Append("</td></tr>");
            sb.Append("<tr><td>Dentista</td><td>").Append(c.DentistaNombre).Append("</td></tr>");
            sb.Append("<tr><td>Tratamiento</td><td>").Append(c.Tratamiento).Append("</td></tr>");
            sb.Append("<tr><td>Cita</td><td>").Append(c.FechaCita.ToString("dd/MM/yyyy HH:mm", cultura))
              .Append(" (").Append(c.DuracionMinutos).Append(" min) — ").Append(c.EstadoCita).Append("</td></tr>");
            sb.Append("<tr><td>Método de pago</td><td>").Append(c.MetodoPago).Append("</td></tr>");
            sb.Append("<tr><td>Estado del pago</td><td>").Append(c.EstadoPago).Append("</td></tr>");
            sb.Append("<tr><td>Fecha de pago</td><td>").Append(c.FechaPago.ToString("dd/MM/yyyy HH:mm", cultura)).Append("</td></tr>");
            sb.Append("</table>");
            sb.Append("<p class=\"total\">Total: ").Append(c.Monto.ToString("C", cultura)).Append("</p>");
            sb.Append("<p>Gracias por su pago.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private async Task<string?> EscribirComprobanteAsync()
        {
            if (_comprobante is null)
                return null;
            var html = GenerarHtmlComprobante();
            var ruta = Path.Combine(FileSystem.CacheDirectory, $"comprobante-{_comprobante.Folio}.html");
            await File.WriteAllTextAsync(ruta, html, Encoding.UTF8);
            return ruta;
        }

        private async Task CompartirAsync()
        {
            var ruta = await EscribirComprobanteAsync();
            if (ruta is null) return;
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Comprobante {_comprobante!.Folio}",
                File = new ShareFile(ruta),
            });
        }

        private async Task ImprimirAsync()
        {
            var ruta = await EscribirComprobanteAsync();
            if (ruta is null) return;
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = $"Comprobante {_comprobante!.Folio}",
                File = new ReadOnlyFile(ruta),
            });
        }
    }
}
