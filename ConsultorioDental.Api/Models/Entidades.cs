// Models/Entidades.cs — Entidades EF Core, 1:1 con ConsultorioDental.sql
// Nombres de tabla y columna = los del script, para que EF Core lea
// la base creada por ese script sin migraciones.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsultorioDental.Api.Models;

[Table("Dentistas")]
public class Dentista
{
    [Key] public int DentistaId { get; set; }
    [Required, MaxLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Apellido { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Especialidad { get; set; } = string.Empty;
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}

[Table("Pacientes")]
public class Paciente
{
    [Key] public int PacienteId { get; set; }
    [Required, MaxLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Apellido { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}

[Table("Tratamientos")]
public class Tratamiento
{
    [Key] public int TratamientoId { get; set; }
    [Required, MaxLength(150)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(500)] public string? Descripcion { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; } = 30;
    public bool Activo { get; set; } = true;
    public ICollection<CitaTratamiento> CitaTratamientos { get; set; } = new List<CitaTratamiento>();
}

[Table("Citas")]
public class Cita
{
    [Key] public int CitaId { get; set; }
    public int PacienteId { get; set; }
    public int DentistaId { get; set; }
    public DateTime FechaHora { get; set; }
    public int DuracionMinutos { get; set; } = 30;
    [Required, MaxLength(20)] public string Estado { get; set; } = "Programada";
    [MaxLength(250)] public string? Motivo { get; set; }
    [MaxLength(500)] public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Paciente Paciente { get; set; } = null!;
    public Dentista Dentista { get; set; } = null!;
    public ICollection<CitaTratamiento> CitaTratamientos { get; set; } = new List<CitaTratamiento>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

[Table("CitaTratamientos")]
public class CitaTratamiento
{
    [Key] public int CitaTratamientoId { get; set; }
    public int CitaId { get; set; }
    public int TratamientoId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Precio { get; set; }
    public int Cantidad { get; set; } = 1;

    public Cita Cita { get; set; } = null!;
    public Tratamiento Tratamiento { get; set; } = null!;
}

[Table("Pagos")]
public class Pago
{
    [Key] public int PagoId { get; set; }
    public int CitaId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    [MaxLength(30)] public string MetodoPago { get; set; } = "Efectivo";
    [MaxLength(20)] public string Estado { get; set; } = "Pagado";

    public Cita Cita { get; set; } = null!;
}

// Usuario del login (2026-11-20). PasswordHash lo genera
// PasswordHasher de ASP.NET Core: aquí nunca se guarda la
// contraseña en texto. Rol: "Admin" (puede eliminar y crear
// usuarios) o "Recepcionista" (opera el día a día, no elimina).
[Table("Usuarios")]
public class Usuario
{
    [Key] public int UsuarioId { get; set; }
    [Required, MaxLength(100)] public string NombreUsuario { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Rol { get; set; } = "Recepcionista";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

// Bitácora de cambios (2026-11-21): quién hizo cada cambio, cuándo
// y sobre qué. UsuarioNombre es una foto del nombre al momento del
// cambio. Los usuarios nunca se borran (solo se desactivan), así
// que UsuarioId siempre apunta a un usuario que existe.
[Table("Bitacora")]
public class Bitacora
{
    [Key] public int BitacoraId { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    [Required, MaxLength(100)] public string UsuarioNombre { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Accion { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Entidad { get; set; } = string.Empty;
    public int EntidadId { get; set; }
    [MaxLength(500)] public string Detalle { get; set; } = string.Empty;

    public Usuario Usuario { get; set; } = null!;
}
