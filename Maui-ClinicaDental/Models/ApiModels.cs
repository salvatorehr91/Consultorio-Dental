// ApiModels.cs — DTOs que devuelve el API REST (.NET MAUI)
// Contratos JSON (camelCase, como los emite ASP.NET Core por defecto).
// Coinciden con ConsultorioDental.Api/Dtos.cs del lado del servidor
// y con las tablas dbo.Pacientes, dbo.Dentistas, dbo.Tratamientos,
// dbo.Citas y dbo.Pagos de ConsultorioDental.sql.
// ACTUALIZADO 2026-11-12 (pagos): PagoDto, CrearPagoRequest y
// CambiarEstadoRequest, todos con [JsonPropertyName] en camelCase.
//
// No redefinen NuevaCitaRequest / NuevoPacienteRequest: esos records
// ya existen en NuevaCitaPage.xaml.cs y NuevoPacientePage.xaml.cs y
// ApiService los reutiliza tal cual.

using System.Text.Json.Serialization;

namespace Maui_ClinicaDental.Models;

// GET /api/pacientes — un paciente de dbo.Pacientes.
public record PacienteDto(
    [property: JsonPropertyName("pacienteId")] int PacienteId,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("apellido")] string Apellido,
    [property: JsonPropertyName("fechaNacimiento")] DateTime? FechaNacimiento,
    [property: JsonPropertyName("telefono")] string? Telefono,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("direccion")] string? Direccion)
{
    [JsonIgnore] public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}

// GET /api/dentistas — un dentista de dbo.Dentistas.
public record DentistaDto(
    [property: JsonPropertyName("dentistaId")] int DentistaId,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("apellido")] string Apellido,
    [property: JsonPropertyName("especialidad")] string Especialidad,
    [property: JsonPropertyName("telefono")] string? Telefono,
    [property: JsonPropertyName("email")] string? Email)
{
    [JsonIgnore] public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}

// GET /api/tratamientos — un tratamiento del catálogo dbo.Tratamientos.
public record TratamientoDto(
    [property: JsonPropertyName("tratamientoId")] int TratamientoId,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("descripcion")] string? Descripcion,
    [property: JsonPropertyName("precio")] decimal Precio,
    [property: JsonPropertyName("duracionMinutos")] int DuracionMinutos);

// GET /api/citas, GET /api/citas/hoy — una cita con los nombres ya
// resueltos por el servidor (JOIN a Pacientes, Dentistas y el primer
// tratamiento de CitaTratamientos). "Tratamiento" es el nombre del
// tratamiento o, si no hay, el Motivo de la cita.
public record CitaDto(
    [property: JsonPropertyName("citaId")] int CitaId,
    [property: JsonPropertyName("pacienteId")] int PacienteId,
    [property: JsonPropertyName("dentistaId")] int DentistaId,
    [property: JsonPropertyName("pacienteNombre")] string PacienteNombre,
    [property: JsonPropertyName("dentistaNombre")] string DentistaNombre,
    [property: JsonPropertyName("fechaHora")] DateTime FechaHora,
    [property: JsonPropertyName("duracionMinutos")] int DuracionMinutos,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("tratamiento")] string Tratamiento,
    [property: JsonPropertyName("motivo")] string? Motivo,
    [property: JsonPropertyName("notas")] string? Notas);

// GET /api/pagos — un pago de dbo.Pagos, ya con el paciente,
// el tratamiento y la fecha de la cita resueltos por el servidor,
// para que el historial no tenga que hacer otra llamada por pago.
public record PagoDto(
    [property: JsonPropertyName("pagoId")] int PagoId,
    [property: JsonPropertyName("citaId")] int CitaId,
    [property: JsonPropertyName("monto")] decimal Monto,
    [property: JsonPropertyName("fechaPago")] DateTime FechaPago,
    [property: JsonPropertyName("metodoPago")] string MetodoPago,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("pacienteNombre")] string PacienteNombre = "",
    [property: JsonPropertyName("tratamiento")] string Tratamiento = "",
    [property: JsonPropertyName("fechaCita")] DateTime? FechaCita = null,
    [property: JsonPropertyName("pacienteId")] int PacienteId = 0)
{
    // Solo para mostrar en la UI, no viajan en el JSON.
    [JsonIgnore] public string MontoTexto => $"${Monto:0.00}";
    [JsonIgnore] public string FechaTexto => FechaPago.ToString("dd/MM/yyyy HH:mm");
    [JsonIgnore] public string FechaCitaTexto => FechaCita?.ToString("dd/MM/yyyy HH:mm") ?? "—";
}

// GET /api/pagos/{id}/comprobante — todo lo del recibo en un objeto, incluidos
// los datos del consultorio (sección "Consultorio" de appsettings.json).
public record ComprobanteDto(
    [property: JsonPropertyName("pagoId")] int PagoId,
    [property: JsonPropertyName("citaId")] int CitaId,
    [property: JsonPropertyName("folio")] string Folio,
    [property: JsonPropertyName("pacienteNombre")] string PacienteNombre,
    [property: JsonPropertyName("dentistaNombre")] string DentistaNombre,
    [property: JsonPropertyName("tratamiento")] string Tratamiento,
    [property: JsonPropertyName("fechaCita")] DateTime FechaCita,
    [property: JsonPropertyName("duracionMinutos")] int DuracionMinutos,
    [property: JsonPropertyName("estadoCita")] string EstadoCita,
    [property: JsonPropertyName("monto")] decimal Monto,
    [property: JsonPropertyName("metodoPago")] string MetodoPago,
    [property: JsonPropertyName("estadoPago")] string EstadoPago,
    [property: JsonPropertyName("fechaPago")] DateTime FechaPago,
    [property: JsonPropertyName("consultorioNombre")] string ConsultorioNombre = "Consultorio Dental",
    [property: JsonPropertyName("consultorioDireccion")] string ConsultorioDireccion = "",
    [property: JsonPropertyName("consultorioTelefono")] string ConsultorioTelefono = "",
    [property: JsonPropertyName("consultorioEmail")] string ConsultorioEmail = "");

// Lo que se envía al API al registrar un pago y al cambiar el estado.
// POST /api/pagos: Estado opcional, por defecto "Pagado".
// OJO: los requests también llevan [JsonPropertyName], igual que los
// DTOs de respuesta. Sin eso, PostAsJsonAsync/PutAsJsonAsync mandan
// "CitaId"/"Monto" en PascalCase; el API lo acepta porque su binding
// no distingue mayúsculas, pero el contrato queda ambiguo y se rompe
// si se serializa a mano. Con el atributo, el JSON que sale es
// exactamente {"citaId":1,"monto":800,"metodoPago":"Efectivo","estado":"Pagado"}.
public record CrearPagoRequest(
    [property: JsonPropertyName("citaId")] int CitaId,
    [property: JsonPropertyName("monto")] decimal Monto,
    [property: JsonPropertyName("metodoPago")] string MetodoPago,
    [property: JsonPropertyName("estado")] string? Estado = "Pagado");

// PUT /api/citas/{id}/estado: { "estado": "Completada" }
public record CambiarEstadoRequest(
    [property: JsonPropertyName("estado")] string Estado);

// Conversiones DTO -> modelos que ya usan las páginas.
public static class ApiModelExtensions
{
    public static PacienteItem ToPacienteItem(this PacienteDto d) => new()
    {
        Id = d.PacienteId,
        Nombre = d.Nombre,
        Apellido = d.Apellido,
        FechaNacimiento = d.FechaNacimiento,
        Telefono = d.Telefono,
        Email = d.Email,
        Direccion = d.Direccion,
    };

    public static CitaItem ToCitaItem(this CitaDto d) => new()
    {
        Id = d.CitaId,
        PacienteId = d.PacienteId,
        FechaHora = d.FechaHora,
        DuracionMinutos = d.DuracionMinutos,
        PacienteNombre = d.PacienteNombre,
        Tratamiento = d.Tratamiento,
        DentistaNombre = d.DentistaNombre,
        Estado = d.Estado,
        Notas = d.Notas,
    };

    public static CitaHoy ToCitaHoy(this CitaDto d) => new(
        d.FechaHora.ToString("HH:mm"),
        $"{d.DuracionMinutos} min",
        d.PacienteNombre,
        d.Tratamiento,
        d.DentistaNombre,
        d.Estado,
        d.Estado switch
        {
            "Confirmada" => "#0E7C7B",
            "Completada" => "#0F3556",
            "Cancelada" => "#B3261E",
            "NoAsistio" => "#8A6D00",
            _ => "#0F3556",
        });

    public static OpcionPersona ToOpcionPersona(this PacienteDto d) => new(d.PacienteId, d.NombreCompleto);
    public static OpcionPersona ToOpcionPersona(this DentistaDto d) => new(d.DentistaId, d.NombreCompleto);
    public static OpcionTratamiento ToOpcionTratamiento(this TratamientoDto t) => new(t.TratamientoId, t.Nombre, t.Precio, t.DuracionMinutos);
}
