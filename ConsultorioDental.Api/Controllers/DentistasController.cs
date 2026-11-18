// Controllers/DentistasController.cs
// GET    /api/dentistas
// GET    /api/dentistas/{id}
// POST   /api/dentistas

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DentistasController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    public DentistasController(ConsultorioDbContext db) => _db = db;

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

        return CreatedAtAction(nameof(GetById), new { id = dentista.DentistaId }, ToDto(dentista));
    }
}
