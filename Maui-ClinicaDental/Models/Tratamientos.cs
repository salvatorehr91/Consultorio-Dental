namespace Maui_ClinicaDental.Models
{
    internal class Tratamientos
    {
        public int TratamientoId { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int DuracionMinutos { get; set; }
        public bool Activo { get; set; }
    }
    public record OpcionPersona(int Id, string NombreCompleto);
    public record OpcionTratamiento(int Id, string Nombre, decimal Precio, int DuracionMinutos);

}
