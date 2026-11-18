// ApiService.cs — Consumo del API REST desde .NET MAUI
// Un solo HttpClient para toda la app. Los ViewModels ya no crean
// su propio HttpClient ni guardan su propia ApiBaseUrl.
//
// URL base según dónde corre la app (¡localhost no es lo mismo en
// todos lados!):
//   Windows / macOS / simulador iOS : http://localhost:5000/api/
//   Emulador Android                : http://10.0.2.2:5000/api/
//     (10.0.2.2 es el alias del emulador para el localhost del PC)
//   Dispositivo físico              : http://<IP-de-tu-PC>:5000/api/
//     (cambia ApiService.BaseUrlOverride por esa IP y abre el puerto
//      5000 en el firewall; el API debe escuchar en 0.0.0.0)
//
// Se usa HTTP en desarrollo para no pelear con el certificado
// autofirmado de HTTPS. En producción, cambia a https://tu-dominio/api/.
//
// Si el API no responde, los métodos GET devuelven null y dejan el
// motivo en UltimoError; los POST devuelven (false, motivo). Las
// páginas muestran entonces los datos de ejemplo y el aviso, en vez
// de romperse. Nunca se lanza la excepción al ViewModel.

using Maui_ClinicaDental.Models;
using System.Net.Http.Json;

namespace Maui_ClinicaDental.Service
{
    public class ApiService
    {
        public static string BaseUrlOverride { get; set; } = string.Empty;
        public static string BaseUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(BaseUrlOverride))
                    return BaseUrlOverride.TrimEnd('/') + "/";

#if ANDROID
                return "http://10.0.2.2:5000/";
#else
                return "https://localhost:65350/";
#endif
            }
        }

        private readonly HttpClient _http;
        public string? UltimoError { get; private set; }

        public ApiService(HttpClient? http = null)
        {
            _http = http ?? new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };
        }

        // GET /api/citas/hoy
        public async Task<List<CitaDto>?> GetCitasHoyAsync()
            => await GetAsync<List<CitaDto>>("citas/hoy");

        // GET /api/citas?fecha=yyyy-MM-dd&estado=Programada
        // Sin fecha trae todas; estado "Todas" se omite.
        public async Task<List<CitaDto>?> GetCitasAsync(DateTime? fecha = null, string? estado = null)
        {
            var query = new List<string>();
            if (fecha.HasValue) query.Add($"fecha={fecha.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todas")
                query.Add($"estado={Uri.EscapeDataString(estado)}");
            var url = query.Count == 0 ? "citas" : "citas?" + string.Join("&", query);
            return await GetAsync<List<CitaDto>>(url);
        }

        // GET /api/pacientes?buscar=texto
        public async Task<List<PacienteDto>?> GetPacientesAsync(string? buscar = null)
        {
            var url = string.IsNullOrWhiteSpace(buscar)
                ? "pacientes"
                : $"pacientes?buscar={Uri.EscapeDataString(buscar)}";
            return await GetAsync<List<PacienteDto>>(url);
        }

        // GET /api/pacientes/count
        public async Task<int?> GetPacientesCountAsync()
        {
            var count = await GetAsync<int>("pacientes/count");
            return count;
        }

        // GET /api/dentistas
        public async Task<List<DentistaDto>?> GetDentistasAsync()
            => await GetAsync<List<DentistaDto>>("dentistas");

        // GET /api/tratamientos
        public async Task<List<TratamientoDto>?> GetTratamientosAsync()
            => await GetAsync<List<TratamientoDto>>("tratamientos");

        // POST /api/citas — devuelve (true, null) si el servidor la creó
        // (201 Created) o (false, motivo) si no.
        public async Task<(bool Ok, string? Error)> CrearCitaAsync(NuevaCitaRequest request)
            => await PostAsync("citas", request);

        // POST /api/pacientes
        public async Task<(bool Ok, string? Error)> CrearPacienteAsync(NuevoPacienteRequest request)
            => await PostAsync("pacientes", request);

        // GET /api/pagos?citaId=1&pacienteId=1&desde=2026-11-01&hasta=2026-11-30&metodo=Efectivo
        public async Task<List<PagoDto>?> GetPagosAsync(
            int? citaId = null, DateTime? desde = null, DateTime? hasta = null, string? metodo = null,
            int? pacienteId = null)
        {
            var query = new List<string>();
            if (citaId.HasValue) query.Add($"citaId={citaId.Value}");
            if (pacienteId.HasValue) query.Add($"pacienteId={pacienteId.Value}");
            if (desde.HasValue) query.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) query.Add($"hasta={hasta.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(metodo) && metodo != "Todos")
                query.Add($"metodo={Uri.EscapeDataString(metodo)}");
            var url = query.Count == 0 ? "pagos" : "pagos?" + string.Join("&", query);
            return await GetAsync<List<PagoDto>>(url);
        }

        // GET /api/pagos/{id}/comprobante
        public async Task<ComprobanteDto?> GetComprobanteAsync(int pagoId)
            => await GetAsync<ComprobanteDto>($"pagos/{pagoId}/comprobante");

        // POST /api/pagos — devuelve (true, null) si se registró (201).
        public async Task<(bool Ok, string? Error)> RegistrarPagoAsync(CrearPagoRequest request)
            => await PostAsync("pagos", request);

        // PUT /api/citas/{id}/estado — cambia solo el Estado.
        public async Task<(bool Ok, string? Error)> CambiarEstadoCitaAsync(int citaId, string estado)
            => await PutAsync($"citas/{citaId}/estado", new CambiarEstadoRequest(estado));

        private async Task<T?> GetAsync<T>(string url)
        {
            UltimoError = null;
            try
            {
                var resultado = await _http.GetFromJsonAsync<T>(url);
                return resultado;
            }
            catch (HttpRequestException ex)
            {
                UltimoError = $"No se pudo conectar con el API ({_http.BaseAddress}{url}). Revisa que el API esté corriendo. Detalle: {ex.Message}";
                return default;
            }
            catch (TaskCanceledException)
            {
                UltimoError = "El API tardó demasiado en responder (timeout de 10 s).";
                return default;
            }
            catch (Exception ex)
            {
                UltimoError = $"Respuesta inesperada del API: {ex.Message}";
                return default;
            }
        }

        private async Task<(bool Ok, string? Error)> PostAsync<T>(string url, T body)
        {
            try
            {
                var respuesta = await _http.PostAsJsonAsync(url, body);
                if (respuesta.IsSuccessStatusCode)
                    return (true, null);

                var detalle = await respuesta.Content.ReadAsStringAsync();
                return (false, $"El servidor no guardó el registro ({(int)respuesta.StatusCode}). {detalle}".Trim());
            }
            catch (HttpRequestException ex)
            {
                return (false, $"No se pudo conectar con el API. Revisa que esté corriendo. Detalle: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "El API tardó demasiado en responder (timeout de 10 s).");
            }
        }

        private async Task<(bool Ok, string? Error)> PutAsync<T>(string url, T body)
        {
            try
            {
                var respuesta = await _http.PutAsJsonAsync(url, body);
                if (respuesta.IsSuccessStatusCode)
                    return (true, null);

                var detalle = await respuesta.Content.ReadAsStringAsync();
                return (false, $"El servidor no actualizó el registro ({(int)respuesta.StatusCode}). {detalle}".Trim());
            }
            catch (HttpRequestException ex)
            {
                return (false, $"No se pudo conectar con el API. Revisa que esté corriendo. Detalle: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "El API tardó demasiado en responder (timeout de 10 s).");
            }
        }
    }
}