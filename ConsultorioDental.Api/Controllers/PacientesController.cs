// Controllers/PacientesController.cs
// GET    /api/pacientes?buscar=maría  — lista (con búsqueda opcional)
// GET    /api/pacientes/count         — total (lo usa el resumen de MainPage)
// GET    /api/pacientes/{id}
// POST   /api/pacientes              — crea (201 Created)
// PUT    /api/pacientes/{id}         — edita (200 con el paciente)
// DELETE /api/pacientes/{id}         — baja lógica (Activo = false, 204).
//        No se borra la fila: sus citas y pagos quedan intactos en
//        el historial. La lista (GET) solo muestra los activos.

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
public class PacientesController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    public PacientesController(ConsultorioDbContext db) => _db = db;

    private static PacienteDto ToDto(Paciente p) => new(
        p.PacienteId, p.Nombre, p.Apellido, p.FechaNacimiento, p.Telefono, p.Email, p.Direccion);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> Get([FromQuery] string? buscar)
    {
        var query = _db.Pacientes.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(p =>
                (p.Nombre + " " + p.Apellido).Contains(texto) ||
                (p.Telefono ?? "").Contains(texto));
        }

        // ToListAsync primero y el mapeo a DTO después: EF Core no
        // puede traducir ToDto() a SQL.
        var pacientes = await query
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .ToListAsync();

        return Ok(pacientes.Select(ToDto));
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> Count()
        => Ok(await _db.Pacientes.CountAsync(p => p.Activo));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PacienteDto>> GetById(int id)
    {
        var paciente = await _db.Pacientes.FindAsync(id);
        return paciente is null ? NotFound() : Ok(ToDto(paciente));
    }

    [HttpPost]
    public async Task<ActionResult<PacienteDto>> Post(CrearPacienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return BadRequest("Nombre y Apellido son obligatorios.");

        var paciente = new Paciente
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Telefono = dto.Telefono,
            Email = dto.Email,
            Direccion = dto.Direccion,
        };

        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = paciente.PacienteId }, ToDto(paciente));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PacienteDto>> Put(int id, ActualizarPacienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return BadRequest("Nombre y Apellido son obligatorios.");

        var paciente = await _db.Pacientes.FindAsync(id);
        if (paciente is null)
            return NotFound();

        paciente.Nombre = dto.Nombre.Trim();
        paciente.Apellido = dto.Apellido.Trim();
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Telefono = dto.Telefono;
        paciente.Email = dto.Email;
        paciente.Direccion = dto.Direccion;

        await _db.SaveChangesAsync();
        return Ok(ToDto(paciente));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var paciente = await _db.Pacientes.FindAsync(id);
        if (paciente is null || !paciente.Activo)
            return NotFound();

        paciente.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
