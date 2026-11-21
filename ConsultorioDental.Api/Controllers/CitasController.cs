// Controllers/CitasController.cs
// GET    /api/citas?fecha=2026-11-10&estado=Confirmada
// GET    /api/citas/hoy
// GET    /api/citas/{id}
// POST   /api/citas — crea la cita y, si viene TratamientoId,
// PUT    /api/citas/{id}         — edita la cita completa (200)
// PUT    /api/citas/{id}/estado — cambia solo el Estado
//        (Programada/Confirmada/Completada/Cancelada/NoAsistio).
// DELETE /api/citas/{id}         — borra la cita (204). Solo si no
//        tiene pagos: si tiene, devuelve 409 para no romper el
//        historial de pagos. Sus filas de CitaTratamientos se borran
//        junto con ella.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CitasController : ControllerBase
{
    private static readonly string[] EstadosValidos =
        { "Programada", "Confirmada", "Completada", "Cancelada", "NoAsistio" };

    // Zona horaria UTC-6 (centro de México). DateTime.Today usa la zona
    // del sistema operativo del servidor; si está en UTC, "hoy" cambia
    // 6 horas antes que en CDMX. Por eso "hoy" se calcula en UTC y se
    // convierte a UTC-6. (Windows: "Central Standard Time"; Linux:
    // "America/Mexico_City".) Lo guardado no cambia: FechaPago sigue
    // en UTC y la app lo muestra con ToLocalTime().
    private static readonly TimeZoneInfo ZonaMexico = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Central Standard Time" : "America/Mexico_City");

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
        c.Notas,
        c.CitaTratamientos.FirstOrDefault()?.TratamientoId);

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

    // PUT /api/citas/{id} — edita paciente, dentista, fecha/hora,
    // duración, estado, motivo y notas. Mismas validaciones que al
    // crear. Si viene TratamientoId, se reemplaza el tratamiento de
    // la cita por ese (al precio del catálogo en ese momento); si no
    // viene, el tratamiento queda como estaba.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CitaDto>> Put(int id, ActualizarCitaDto dto)
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

        var cita = await _db.Citas
            .Include(c => c.CitaTratamientos)
            .FirstOrDefaultAsync(c => c.CitaId == id);
        if (cita is null)
            return NotFound();

        cita.PacienteId = dto.PacienteId;
        cita.DentistaId = dto.DentistaId;
        cita.FechaHora = dto.FechaHora;
        cita.DuracionMinutos = dto.DuracionMinutos;
        cita.Estado = dto.Estado;
        cita.Motivo = dto.Motivo;
        cita.Notas = dto.Notas;

        if (tratamiento is not null)
        {
            _db.CitaTratamientos.RemoveRange(cita.CitaTratamientos);
            _db.CitaTratamientos.Add(new CitaTratamiento
            {
                CitaId = cita.CitaId,
                TratamientoId = tratamiento.TratamientoId,
                Precio = tratamiento.Precio,
                Cantidad = 1,
            });
        }

        await _db.SaveChangesAsync();

        var actualizada = await QueryBase().FirstAsync(c => c.CitaId == id);
        return Ok(ToDto(actualizada));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cita = await _db.Citas
            .Include(c => c.CitaTratamientos)
            .FirstOrDefaultAsync(c => c.CitaId == id);
        if (cita is null)
            return NotFound();

        // Una cita con pagos no se borra: el pago quedaría huérfano y
        // el historial de PagosPage perdería su cita. En ese caso se
        // sugiere cambiar su estado a Cancelada en vez de borrarla.
        if (await _db.Pagos.AnyAsync(p => p.CitaId == id))
            return Conflict("No se puede eliminar la cita porque ya tiene pagos registrados. Cambia su estado a Cancelada en su lugar.");

        _db.CitaTratamientos.RemoveRange(cita.CitaTratamientos);
        _db.Citas.Remove(cita);
        await _db.SaveChangesAsync();
        return NoContent();
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
