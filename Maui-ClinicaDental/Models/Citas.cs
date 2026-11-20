namespace Maui_ClinicaDental.Models
{
    internal class Citas
    {
        public int CitaId { get; set; }
        public int PacienteId { get; set; }
        public int DentistaId { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public required string Estado { get; set; }
        public required string Motivo { get; set; }
        public string? Notas { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class CitaItem
    {
        public int Id { get; init; }
        // CORRECCIÓN 2026-11-14: faltaba el id del paciente. Sin él,
        // "Ver pagos" desde Citas solo podía mandar el nombre.
        public int PacienteId { get; init; }
        // 2026-11-19 (editar cita): faltaban el id del dentista, el id
        // del tratamiento y el motivo, que el formulario de edición
        // necesita para precargar los pickers. Vienen del API en CitaDto.
        public int DentistaId { get; init; }
        public int? TratamientoId { get; init; }
        public DateTime FechaHora { get; init; }
        public int DuracionMinutos { get; init; }
        public string PacienteNombre { get; init; } = string.Empty;
        public string Tratamiento { get; init; } = string.Empty;
        public string DentistaNombre { get; init; } = string.Empty;
        public string Estado { get; init; } = "Programada";
        public string? Motivo { get; init; }
        public string? Notas { get; init; }

        public string Hora => FechaHora.ToString("HH:mm");
        public string FechaCorta => FechaHora.ToString("dd/MM");
        public string Duracion => $"{DuracionMinutos} min";
        public string EstadoColor => Estado switch
        {
            "Confirmada" => "#0E7C7B",
            "Completada" => "#0F3556",
            "Cancelada" => "#B3261E",
            "NoAsistio" => "#8A6D00",
            _ => "#0F3556", // Programada
        };
    }

    public record NuevaCitaRequest(
    int PacienteId,
    int DentistaId,
    DateTime FechaHora,
    int DuracionMinutos,
    string Estado,
    string? Motivo,
    string? Notas,
    int? TratamientoId);
}
