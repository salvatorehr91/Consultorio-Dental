namespace Maui_ClinicaDental.Models
{
    internal class CitasHoy
    {
        public TimeOnly Hora { get; set; }
        public int Duracion { get; set; }
        public required string PacienteNombre { get; set; }
        public required string Tratamiento { get; set; }
        public required string DentistaNombre { get; set; }
        public bool Estado { get; set; }
        public required string EstadoColor { get; set; }
    }
    public record CitaHoy(
    string Hora,
    string Duracion,
    string PacienteNombre,
    string Tratamiento,
    string DentistaNombre,
    string Estado,
    string EstadoColor);
}
