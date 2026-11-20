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
                // CORRECCIÓN 2026-11-18: antes decía https://10.0.2.3:5000/
                // (10.0.2.3 es el gateway, no tu PC, y https con el
                // certificado de desarrollo no lo acepta Android).
                // Emulador: 10.0.2.2 es el alias del localhost de tu PC,
                // en HTTP. El API debe correr en http://0.0.0.0:5000.
                // Dispositivo físico: cambia 10.0.2.2 por la IP LAN de
                // tu PC, p. ej. http://192.168.1.50:5000/
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

        // GET /api/pacientes/{id} — para precargar el formulario de edición.
        public async Task<PacienteDto?> GetPacienteAsync(int id)
            => await GetAsync<PacienteDto>($"pacientes/{id}");

        // GET /api/citas/{id} — para precargar el formulario de edición.
        public async Task<CitaDto?> GetCitaAsync(int id)
            => await GetAsync<CitaDto>($"citas/{id}");

        // GET /api/dentistas/{id} y GET /api/tratamientos/{id}.
        public async Task<DentistaDto?> GetDentistaAsync(int id)
            => await GetAsync<DentistaDto>($"dentistas/{id}");

        public async Task<TratamientoDto?> GetTratamientoAsync(int id)
            => await GetAsync<TratamientoDto>($"tratamientos/{id}");

        // POST /api/dentistas y POST /api/tratamientos.
        // CrearDentistaRequest / CrearTratamientoRequest son los mismos
        // records que Actualizar*Request: el servidor usa los mismos
        // campos para crear y para editar, así que se reutilizan.
        public async Task<(bool Ok, string? Error)> CrearDentistaAsync(ActualizarDentistaRequest request)
            => await PostAsync("dentistas", request);

        public async Task<(bool Ok, string? Error)> CrearTratamientoAsync(ActualizarTratamientoRequest request)
            => await PostAsync("tratamientos", request);

        // PUT /api/pacientes/{id}, PUT /api/dentistas/{id},
        // PUT /api/tratamientos/{id} y PUT /api/citas/{id}.
        public async Task<(bool Ok, string? Error)> ActualizarPacienteAsync(int id, ActualizarPacienteRequest request)
            => await PutAsync($"pacientes/{id}", request);

        public async Task<(bool Ok, string? Error)> ActualizarDentistaAsync(int id, ActualizarDentistaRequest request)
            => await PutAsync($"dentistas/{id}", request);

        public async Task<(bool Ok, string? Error)> ActualizarTratamientoAsync(int id, ActualizarTratamientoRequest request)
            => await PutAsync($"tratamientos/{id}", request);

        public async Task<(bool Ok, string? Error)> ActualizarCitaAsync(int id, ActualizarCitaRequest request)
            => await PutAsync($"citas/{id}", request);

        // DELETE /api/pacientes/{id} — baja lógica en el servidor
        // (Activo = false); la fila y su historial quedan intactos.
        public async Task<(bool Ok, string? Error)> EliminarPacienteAsync(int id)
            => await DeleteAsync($"pacientes/{id}");

        // DELETE /api/dentistas/{id} y DELETE /api/tratamientos/{id}:
        // también baja lógica (Activo = false).
        public async Task<(bool Ok, string? Error)> EliminarDentistaAsync(int id)
            => await DeleteAsync($"dentistas/{id}");

        public async Task<(bool Ok, string? Error)> EliminarTratamientoAsync(int id)
            => await DeleteAsync($"tratamientos/{id}");

        // DELETE /api/citas/{id} — borrado real. Si la cita tiene
        // pagos, el servidor devuelve 409 y el mensaje explica que
        // hay que cancelarla en vez de borrarla.
        public async Task<(bool Ok, string? Error)> EliminarCitaAsync(int id)
            => await DeleteAsync($"citas/{id}");

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

        // Reportes (2026-11-19). Desde/Hasta van como yyyy-MM-dd
        // (fecha local del consultorio); si no se pasan, el servidor
        // usa el mes en curso.
        public async Task<ResumenReporteDto?> GetResumenReportesAsync(DateTime? desde = null, DateTime? hasta = null)
            => await GetAsync<ResumenReporteDto>(ReporteUrl("reportes/resumen", desde, hasta));

        public async Task<List<CobroPorDiaDto>?> GetCobrosPorDiaAsync(DateTime? desde = null, DateTime? hasta = null)
            => await GetAsync<List<CobroPorDiaDto>>(ReporteUrl("reportes/cobros-por-dia", desde, hasta));

        public async Task<List<TratamientoUsoDto>?> GetTratamientosMasUsadosAsync(DateTime? desde = null, DateTime? hasta = null, int top = 10)
        {
            var url = ReporteUrl("reportes/tratamientos-mas-usados", desde, hasta);
            url += (url.Contains('?') ? "&" : "?") + $"top={top}";
            return await GetAsync<List<TratamientoUsoDto>>(url);
        }

        public async Task<List<CitasPorDentistaDto>?> GetCitasPorDentistaAsync(DateTime? desde = null, DateTime? hasta = null)
            => await GetAsync<List<CitasPorDentistaDto>>(ReporteUrl("reportes/citas-por-dentista", desde, hasta));

        private static string ReporteUrl(string ruta, DateTime? desde, DateTime? hasta)
        {
            var query = new List<string>();
            if (desde.HasValue) query.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) query.Add($"hasta={hasta.Value:yyyy-MM-dd}");
            return query.Count == 0 ? ruta : ruta + "?" + string.Join("&", query);
        }

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

        // DELETE — sin cuerpo. El servidor devuelve 204 (sin contenido)
        // al borrar/dar de baja, y 409 con el motivo si no se puede
        // (una cita con pagos). El detalle se devuelve tal cual para
        // mostrarlo en la alerta, sin el código entre paréntesis.
        private async Task<(bool Ok, string? Error)> DeleteAsync(string url)
        {
            try
            {
                var respuesta = await _http.DeleteAsync(url);
                if (respuesta.IsSuccessStatusCode)
                    return (true, null);

                var detalle = (await respuesta.Content.ReadAsStringAsync()).Trim().Trim('"');
                return (false, string.IsNullOrWhiteSpace(detalle)
                    ? $"El servidor no eliminó el registro ({(int)respuesta.StatusCode})."
                    : detalle);
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