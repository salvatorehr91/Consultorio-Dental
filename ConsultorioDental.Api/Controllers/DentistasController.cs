// Controllers/DentistasController.cs
// GET    /api/dentistas
// GET    /api/dentistas/{id}
// POST   /api/dentistas
// PUT    /api/dentistas/{id}         — edita (200 con el dentista)
// DELETE /api/dentistas/{id}         — baja lógica (Activo = false, 204).
//        No se borra la fila: sus citas quedan intactas en el historial.
//        Un dentista dado de baja ya no aparece en la lista ni se le
//        pueden crear citas nuevas (POST /api/citas exige Activo).

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
public class DentistasController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    private readonly BitacoraService _bitacora;
    public DentistasController(ConsultorioDbContext db, BitacoraService bitacora)
    {
        _db = db;
        _bitacora = bitacora;
    }

    private static DentistaDto ToDto(Dentista d) => new(
        d.DentistaId, d.Nombre, d.Apellido, d.Especialidad, d.Telefono, d.Email);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DentistaDto>>> Get()
    {
        var dentistas = await _db.Dentistas.Where(d => d.Activo)
            .OrderBy(d => d.Apellido).ThenBy(d => d.Nombre)
            .ToListAsync();
        return Ok(dentistas.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DentistaDto>> GetById(int id)
    {
        var dentista = await _db.Dentistas.FindAsync(id);
        return dentista is null ? NotFound() : Ok(ToDto(dentista));
    }

    [HttpPost]
    public async Task<ActionResult<DentistaDto>> Post(CrearDentistaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) ||
            string.IsNullOrWhiteSpace(dto.Especialidad))
            return BadRequest("Nombre, Apellido y Especialidad son obligatorios.");

        var dentista = new Dentista
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Especialidad = dto.Especialidad.Trim(),
            Telefono = dto.Telefono,
            Email = dto.Email,
        };

        _db.Dentistas.Add(dentista);
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("Crear", "Dentista", dentista.DentistaId, $"{dentista.Nombre} {dentista.Apellido} — {dentista.Especialidad}".Trim());

        return CreatedAtAction(nameof(GetById), new { id = dentista.DentistaId }, ToDto(dentista));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DentistaDto>> Put(int id, ActualizarDentistaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) ||
            string.IsNullOrWhiteSpace(dto.Especialidad))
            return BadRequest("Nombre, Apellido y Especialidad son obligatorios.");

        var dentista = await _db.Dentistas.FindAsync(id);
        if (dentista is null)
            return NotFound();

        dentista.Nombre = dto.Nombre.Trim();
        dentista.Apellido = dto.Apellido.Trim();
        dentista.Especialidad = dto.Especialidad.Trim();
        dentista.Telefono = dto.Telefono;
        dentista.Email = dto.Email;

        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("Editar", "Dentista", dentista.DentistaId, $"{dentista.Nombre} {dentista.Apellido} — {dentista.Especialidad}".Trim());
        return Ok(ToDto(dentista));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dentista = await _db.Dentistas.FindAsync(id);
        if (dentista is null || !dentista.Activo)
            return NotFound();

        dentista.Activo = false;
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("Eliminar", "Dentista", dentista.DentistaId, $"{dentista.Nombre} {dentista.Apellido} — {dentista.Especialidad}".Trim());
        return NoContent();
    }
}
