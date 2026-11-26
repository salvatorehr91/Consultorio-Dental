// Dtos.cs — Contratos JSON del API (lo que ve la app MAUI)
// ASP.NET Core serializa en camelCase por defecto, así que estos
// nombres en PascalCase llegan como pacienteId, nombre, etc.,
// que es justo lo que ApiModels.cs espera en MAUI.

// Lo que el API devuelve:
namespace ConsultorioDental.Api.Data;

public record PacienteDto(int PacienteId, string Nombre, string Apellido, DateTime? FechaNacimiento, string? Telefono, string? Email, string? Direccion);
public record DentistaDto(int DentistaId, string Nombre, string Apellido, string Especialidad, string? Telefono, string? Email);
public record TratamientoDto(int TratamientoId, string Nombre, string? Descripcion, decimal Precio, int DuracionMinutos);
public record CitaDto(int CitaId, int PacienteId, int DentistaId, string PacienteNombre, string DentistaNombre, DateTime FechaHora, int DuracionMinutos, string Estado, string Tratamiento, string? Motivo, string? Notas, int? TratamientoId = null);
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

// Lo que la app envía al editar (PUT /api/{entidad}/{id}).
// Tienen los mismos campos que los Crear*, para que el formulario
// de edición reutilice las mismas validaciones del servidor.
public record ActualizarPacienteDto(string Nombre, string Apellido, DateTime? FechaNacimiento, string? Telefono, string? Email, string? Direccion);
public record ActualizarDentistaDto(string Nombre, string Apellido, string Especialidad, string? Telefono, string? Email);
public record ActualizarTratamientoDto(string Nombre, string? Descripcion, decimal Precio, int DuracionMinutos);
// En ActualizarCitaDto, TratamientoId es opcional: si viene, se
// reemplaza el tratamiento de la cita por ese (al precio del
// catálogo en ese momento); si no viene, el tratamiento queda igual.
public record ActualizarCitaDto(int PacienteId, int DentistaId, DateTime FechaHora, int DuracionMinutos, string Estado, string? Motivo, string? Notas, int? TratamientoId);


// Estado y reporte de citas y pagos, para los endpoints de ReportesController.
public record EstadoCantidadDto(string Estado, int Cantidad);
public record ResumenReporteDto(decimal TotalCobrado, int CantidadPagos, decimal PromedioPorPago, int TotalCitas, List<EstadoCantidadDto> CitasPorEstado);
public record CobroPorDiaDto(DateTime Fecha, decimal Total, int CantidadPagos);
public record TratamientoUsoDto(int TratamientoId, string Nombre, int Veces, decimal TotalCobrado);
public record CitasPorDentistaDto(int DentistaId, string DentistaNombre, int TotalCitas, int Completadas, int Canceladas);

// Login de administrador (2026-11-20).
public record LoginDto(string NombreUsuario, string Password);
public record LoginRespuestaDto(string Token, string NombreUsuario, string Rol, DateTime Expiracion);
public record CambiarPasswordDto(string PasswordActual, string PasswordNuevo);
public record CrearUsuarioDto(string NombreUsuario, string Password, string Rol);
public record UsuarioDto(int UsuarioId, string NombreUsuario, string Rol, bool Activo);
public record CambiarEstadoUsuarioDto(bool Activo);
public record RestablecerPasswordDto(string PasswordNuevo);
public record BitacoraDto(int BitacoraId, DateTime FechaHora, int UsuarioId, string UsuarioNombre, string Accion, string Entidad, int EntidadId, string Detalle);

// Actualización por APK y aviso de citas nuevas (2026-11-25).
public record VersionAppDto(string Version, string UrlApk, string? Notas);
public record NuevasCitasDto(int Cantidad, int UltimoId);
