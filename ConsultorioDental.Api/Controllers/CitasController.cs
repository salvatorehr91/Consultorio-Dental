// Controllers/CitasController.cs
// GET    /api/citas?fecha=2026-11-10&estado=Confirmada
// GET    /api/citas/hoy
// GET    /api/citas/{id}
// POST   /api/citas — crea la cita y, si viene TratamientoId,
// PUT    /api/citas/{id}/estado — cambia solo el Estado
//        (Programada/Confirmada/Completada/Cancelada/NoAsistio).

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CitasController : ControllerBase
{
    private static readonly string[] EstadosValidos =
        { "Programada", "Confirmada", "Completada", "Cancelada", "NoAsistio" };


    // CORRECCIÓN 2026-11-18 — Zona horaria UTC-6 (centro de México).
    // No hay un "ajuste de zona horaria" en ASP.NET Core: DateTime.Today
    // usa la zona del sistema operativo del servidor, y si el servidor
    // está en UTC, "hoy" cambia 6 horas antes que en CDMX.
    // Por eso "hoy" se calcula en UTC y se convierte a UTC-6.
    // El id de Windows es "Central Standard Time" (UTC-6, sin horario
    // de verano en México desde 2022); en Linux es "America/Mexico_City".
    // NO se cambia lo que se guarda: FechaPago sigue en UTC y la app
    // ya lo muestra con ToLocalTime(); solo cambia qué día es "hoy".
    private static readonly TimeZoneInfo ZonaMexico = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Central Standard Time" : "America/Mexico_City");

    // Día actual en UTC-6.
    private static DateTime HoyEnMexico() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaMexico).Date;

    private readonly ConsultorioDbContext _db;
    public CitasController(ConsultorioDbContext db) => _db = db;

    private static CitaDto ToDto(Cita c) => new(
        c.CitaId,
        c.PacienteId,
        c.DentistaId,
        $"{c.Paciente.Nombre} {c.Paciente.Apellido}".Trim(),
        $"{c.Dentista.Nombre} {c.Dentista.Apellido}".Trim(),
        c.FechaHora,
        c.DuracionMinutos,
        c.Estado,
        c.CitaTratamientos.FirstOrDefault()?.Tratamiento.Nombre ?? c.Motivo ?? string.Empty,
        c.Motivo,
        c.Notas);

    private IQueryable<Cita> QueryBase() => _db.Citas
        .Include(c => c.Paciente)
        .Include(c => c.Dentista)
        .Include(c => c.CitaTratamientos).ThenInclude(ct => ct.Tratamiento);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaDto>>> Get(
        [FromQuery] DateTime? fecha, [FromQuery] string? estado)
    {
        var query = QueryBase();

        if (fecha.HasValue)
            query = query.Where(c => c.FechaHora.Date == fecha.Value.Date);

        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todas")
            query = query.Where(c => c.Estado == estado);

        var citas = await query.OrderBy(c => c.FechaHora).ToListAsync();
        return Ok(citas.Select(ToDto));
    }

     [HttpGet("hoy")]
    public async Task<ActionResult<IEnumerable<CitaDto>>> GetHoy()
    {
        var hoy = HoyEnMexico();
        var citas = await QueryBase()
            .Where(c => c.FechaHora.Date == hoy)
            .OrderBy(c => c.FechaHora)
            .ToListAsync();
        return Ok(citas.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CitaDto>> GetById(int id)
    {
        var cita = await QueryBase().FirstOrDefaultAsync(c => c.CitaId == id);
        return cita is null ? NotFound() : Ok(ToDto(cita));
    }

    // PUT /api/citas/{id}/estado  { "estado": "Completada" }
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<CitaDto>> CambiarEstado(int id, CambiarEstadoDto dto)
    {
        if (!EstadosValidos.Contains(dto.Estado))
            return BadRequest($"Estado no válido. Usa: {string.Join(", ", EstadosValidos)}.");

        var cita = await _db.Citas.FindAsync(id);
        if (cita is null)
            return NotFound();

        cita.Estado = dto.Estado;
        await _db.SaveChangesAsync();

        var actualizada = await QueryBase().FirstAsync(c => c.CitaId == id);
        return Ok(ToDto(actualizada));
    }

    [HttpPost]
    public async Task<ActionResult<CitaDto>> Post(CrearCitaDto dto)
    {
        if (!await _db.Pacientes.AnyAsync(p => p.PacienteId == dto.PacienteId && p.Activo))
            return BadRequest($"No existe el paciente {dto.PacienteId}.");

        if (!await _db.Dentistas.AnyAsync(d => d.DentistaId == dto.DentistaId && d.Activo))
            return BadRequest($"No existe el dentista {dto.DentistaId}.");

        if (!EstadosValidos.Contains(dto.Estado))
            return BadRequest($"Estado no válido. Usa: {string.Join(", ", EstadosValidos)}.");

        if (dto.DuracionMinutos <= 0)
            return BadRequest("DuracionMinutos debe ser mayor que 0.");

        Tratamiento? tratamiento = null;
        if (dto.TratamientoId.HasValue)
        {
            tratamiento = await _db.Tratamientos.FindAsync(dto.TratamientoId.Value);
            if (tratamiento is null)
                return BadRequest($"No existe el tratamiento {dto.TratamientoId}.");
        }

        var cita = new Cita
        {
            PacienteId = dto.PacienteId,
            DentistaId = dto.DentistaId,
            FechaHora = dto.FechaHora,
            DuracionMinutos = dto.DuracionMinutos,
            Estado = dto.Estado,
            Motivo = dto.Motivo,
            Notas = dto.Notas,
        };

        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();

        if (tratamiento is not null)
        {
            _db.CitaTratamientos.Add(new CitaTratamiento
            {
                CitaId = cita.CitaId,
                TratamientoId = tratamiento.TratamientoId,
                Precio = tratamiento.Precio, // precio del catálogo en este momento
                Cantidad = 1,
            });
            await _db.SaveChangesAsync();
        }

        var creada = await QueryBase().FirstAsync(c => c.CitaId == cita.CitaId);
        return CreatedAtAction(nameof(GetById), new { id = cita.CitaId }, ToDto(creada));
    }
}
