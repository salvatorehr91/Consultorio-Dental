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
}
