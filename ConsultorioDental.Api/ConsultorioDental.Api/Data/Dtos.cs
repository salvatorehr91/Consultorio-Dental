// Dtos.cs — Contratos JSON del API (lo que ve la app MAUI)
// ASP.NET Core serializa en camelCase por defecto, así que estos
// nombres en PascalCase llegan como pacienteId, nombre, etc.,
// que es justo lo que ApiModels.cs espera en MAUI.

// Lo que el API devuelve:
namespace ConsultorioDental.Api.Data;

public record PacienteDto(int PacienteId, string Nombre, string Apellido, DateTime? FechaNacimiento, string? Telefono, string? Email, string? Direccion);
public record DentistaDto(int DentistaId, string Nombre, string Apellido, string Especialidad, string? Telefono, string? Email);
public record TratamientoDto(int TratamientoId, string Nombre, string? Descripcion, decimal Precio, int DuracionMinutos);
public record CitaDto(int CitaId, int PacienteId, int DentistaId, string PacienteNombre, string DentistaNombre, DateTime FechaHora, int DuracionMinutos, string Estado, string Tratamiento, string? Motivo, string? Notas);
public record PagoDto(int PagoId, int CitaId, decimal Monto, DateTime FechaPago, string MetodoPago, string Estado, string PacienteNombre, string Tratamiento, DateTime? FechaCita, int PacienteId = 0);
// Comprobante para imprimir: todo lo que lleva el recibo en un solo objeto.
// Los 4 datos del consultorio vienen de appsettings.json, sección "Consultorio",
// para que se cambien en un solo lugar y lleguen igual a la app y al HTML impreso.
public record ComprobanteDto(int PagoId, int CitaId, string Folio, string PacienteNombre, string DentistaNombre, string Tratamiento, DateTime FechaCita, int DuracionMinutos, string EstadoCita, decimal Monto, string MetodoPago, string EstadoPago, DateTime FechaPago, string ConsultorioNombre = "Consultorio Dental", string ConsultorioDireccion = "", string ConsultorioTelefono = "", string ConsultorioEmail = "");

// Lo que la app envía al crear (coincide con NuevaCitaRequest y
// NuevoPacienteRequest de MAUI):
public record CrearPacienteDto(string Nombre, string Apellido, DateTime? FechaNacimiento, string? Telefono, string? Email, string? Direccion);
public record CrearDentistaDto(string Nombre, string Apellido, string Especialidad, string? Telefono, string? Email);
public record CrearTratamientoDto(string Nombre, string? Descripcion, decimal Precio, int DuracionMinutos);
public record CrearCitaDto(int PacienteId, int DentistaId, DateTime FechaHora, int DuracionMinutos, string Estado, string? Motivo, string? Notas, int? TratamientoId);
public record CrearPagoDto(int CitaId, decimal Monto, string MetodoPago, string? Estado);
public record CambiarEstadoDto(string Estado);
