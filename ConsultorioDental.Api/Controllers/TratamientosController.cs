// Controllers/TratamientosController.cs
// GET    /api/tratamientos
// GET    /api/tratamientos/{id}
// POST   /api/tratamientos
// PUT    /api/tratamientos/{id}      — edita (200 con el tratamiento)
// DELETE /api/tratamientos/{id}      — baja lógica (Activo = false, 204).
//        No se borra la fila: el precio cobrado queda guardado en
//        CitaTratamientos, así que el historial no se rompe. Un
//        tratamiento dado de baja ya no aparece en el catálogo.

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
public class TratamientosController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    public TratamientosController(ConsultorioDbContext db) => _db = db;

    private static TratamientoDto ToDto(Tratamiento t) => new(
        t.TratamientoId, t.Nombre, t.Descripcion, t.Precio, t.DuracionMinutos);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TratamientoDto>>> Get()
    {
        var tratamientos = await _db.Tratamientos.Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
        return Ok(tratamientos.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TratamientoDto>> GetById(int id)
    {
        var tratamiento = await _db.Tratamientos.FindAsync(id);
        return tratamiento is null ? NotFound() : Ok(ToDto(tratamiento));
    }

    [HttpPost]
    public async Task<ActionResult<TratamientoDto>> Post(CrearTratamientoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Precio < 0 || dto.DuracionMinutos <= 0)
            return BadRequest("Nombre obligatorio, Precio >= 0 y DuracionMinutos > 0.");

        var tratamiento = new Tratamiento
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            DuracionMinutos = dto.DuracionMinutos,
        };

        _db.Tratamientos.Add(tratamiento);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tratamiento.TratamientoId }, ToDto(tratamiento));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TratamientoDto>> Put(int id, ActualizarTratamientoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Precio < 0 || dto.DuracionMinutos <= 0)
            return BadRequest("Nombre obligatorio, Precio >= 0 y DuracionMinutos > 0.");

        var tratamiento = await _db.Tratamientos.FindAsync(id);
        if (tratamiento is null)
            return NotFound();

        tratamiento.Nombre = dto.Nombre.Trim();
        tratamiento.Descripcion = dto.Descripcion;
        tratamiento.Precio = dto.Precio;
        tratamiento.DuracionMinutos = dto.DuracionMinutos;

        await _db.SaveChangesAsync();
        return Ok(ToDto(tratamiento));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tratamiento = await _db.Tratamientos.FindAsync(id);
        if (tratamiento is null || !tratamiento.Activo)
            return NotFound();

        tratamiento.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
