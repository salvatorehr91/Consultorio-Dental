namespace Maui_ClinicaDental.Models
{
    internal class Dentistas
    {
        public int DentistaId { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public required string Especialidad { get; set; }
        public required string Telefono { get; set; }
        public required string Email { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // 2026-11-19 (DentistasPage): lo que muestra la lista de
    // dentistas, igual que PacienteItem para PacientesPage. Viene de
    // DentistaDto por la conversión ToDentistaItem en ApiModels.cs.
    public class DentistaItem
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string Especialidad { get; init; } = string.Empty;
        public string? Telefono { get; init; }
        public string? Email { get; init; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public string Iniciales => string.Concat(
            string.IsNullOrWhiteSpace(Nombre) ? "" : Nombre[..1],
            string.IsNullOrWhiteSpace(Apellido) ? "" : Apellido[..1]).ToUpperInvariant();
    }
}
