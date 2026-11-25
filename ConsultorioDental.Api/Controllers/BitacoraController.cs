// Controllers/BitacoraController.cs — Consulta de la bitácora (2026-11-21)
// GET    /bitacora?desde=2026-11-01&hasta=2026-11-30&usuarioId=1
//        &entidad=Cita&accion=Eliminar&top=200
//        — registros más recientes primero. Solo Admin.
//        Las fechas llegan como fecha local del consultorio y se
//        comparan en hora de México (FechaHora se guarda en UTC),
//        igual que en ReportesController.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class BitacoraController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    public BitacoraController(ConsultorioDbContext db) => _db = db;

    private static TimeZoneInfo ZonaMexico()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int? usuarioId, [FromQuery] string? entidad,
        [FromQuery] string? accion, [FromQuery] int top = 200)
    {
        if (top < 1) top = 1;
        if (top > 500) top = 500;

        var query = _db.Bitacora.AsQueryable();

        if (desde.HasValue)
        {
            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(desde.Value.Date, ZonaMexico());
            query = query.Where(b => b.FechaHora >= inicioUtc);
        }
        if (hasta.HasValue)
        {
            var finUtc = TimeZoneInfo.ConvertTimeToUtc(hasta.Value.Date.AddDays(1), ZonaMexico());
            query = query.Where(b => b.FechaHora < finUtc);
        }
        if (usuarioId.HasValue)
            query = query.Where(b => b.UsuarioId == usuarioId.Value);
        if (!string.IsNullOrWhiteSpace(entidad) && entidad != "Todas")
            query = query.Where(b => b.Entidad == entidad);
        if (!string.IsNullOrWhiteSpace(accion) && accion != "Todas")
            query = query.Where(b => b.Accion == accion);

        var registros = await query
            .OrderByDescending(b => b.FechaHora)
            .Take(top)
            .Select(b => new BitacoraDto(
                b.BitacoraId, b.FechaHora, b.UsuarioId, b.UsuarioNombre,
                b.Accion, b.Entidad, b.EntidadId, b.Detalle))
            .ToListAsync();
        return Ok(registros);
    }
}
