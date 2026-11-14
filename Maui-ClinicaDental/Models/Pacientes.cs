namespace Maui_ClinicaDental.Models
{
    internal class Pacientes
    {
        public int PacienteId { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public DateOnly FechaNacimiento { get; set; }
        public required string Telefono { get; set; }
        public required string Email { get; set; }
        public required string Direccion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class PacienteItem
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public DateTime? FechaNacimiento { get; init; }
        public string? Telefono { get; init; }
        public string? Email { get; init; }
        public string? Direccion { get; init; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public string Iniciales => string.Concat(
            string.IsNullOrWhiteSpace(Nombre) ? "" : Nombre[..1],
            string.IsNullOrWhiteSpace(Apellido) ? "" : Apellido[..1]).ToUpperInvariant();
    }

    // Lo que se envía al API al crear el paciente.
    public record NuevoPacienteRequest(
        string Nombre,
        string Apellido,
        DateTime? FechaNacimiento,
        string? Telefono,
        string? Email,
        string? Direccion);
}
