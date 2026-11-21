// Controllers/PagosController.cs
// GET    /api/pagos?citaId=1&desde=2026-11-01&hasta=2026-11-30&metodo=Efectivo
//        — historial de pagos, con filtros opcionales. El DTO ya trae
//        PacienteNombre, Tratamiento y FechaCita, para que la lista
//        no tenga que hacer otra llamada por pago.
// GET    /api/pagos/{id}
// GET    /api/pagos/{id}/comprobante — todo lo del recibo en un objeto.
// POST   /api/pagos            — registra un pago en dbo.Pagos.
//        Valida que exista la cita, Monto > 0, MetodoPago y Estado
//        dentro de los CHECK de la tabla. Devuelve 201 Created.
//        El pago NO cambia el Estado de la cita: eso se hace con
//        PUT /api/citas/{id}/estado.

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
public class PagosController : ControllerBase
{
    private static readonly string[] MetodosValidos =
        { "Efectivo", "Tarjeta", "Transferencia", "Otro" };
    private static readonly string[] EstadosValidos =
        { "Pagado", "Pendiente", "Reembolsado" };

    private readonly ConsultorioDbContext _db;
    private readonly IConfiguration _config;
    public PagosController(ConsultorioDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private static PagoDto ToDto(Pago p) => new(
        p.PagoId,
        p.CitaId,
        p.Monto,
        p.FechaPago,
        p.MetodoPago,
        p.Estado,
        $"{p.Cita.Paciente.Nombre} {p.Cita.Paciente.Apellido}".Trim(),
        p.Cita.CitaTratamientos.FirstOrDefault()?.Tratamiento.Nombre ?? p.Cita.Motivo ?? string.Empty,
        p.Cita.FechaHora,
        p.Cita.PacienteId);

    // Datos del consultorio: un solo lugar, appsettings.json -> "Consultorio".
    // Si falta un dato, queda vacío (no se inventa una dirección ni un teléfono).
    private ComprobanteDto ToComprobante(Pago p) => new(
        p.PagoId,
        p.CitaId,
        $"PAG-{p.PagoId:0000}",
        $"{p.Cita.Paciente.Nombre} {p.Cita.Paciente.Apellido}".Trim(),
        $"{p.Cita.Dentista.Nombre} {p.Cita.Dentista.Apellido}".Trim(),
        p.Cita.CitaTratamientos.FirstOrDefault()?.Tratamiento.Nombre ?? p.Cita.Motivo ?? string.Empty,
        p.Cita.FechaHora,
        p.Cita.DuracionMinutos,
        p.Cita.Estado,
        p.Monto,
        p.MetodoPago,
        p.Estado,
        p.FechaPago,
        _config["Consultorio:Nombre"] ?? "Consultorio Dental",
        _config["Consultorio:Direccion"] ?? string.Empty,
        _config["Consultorio:Telefono"] ?? string.Empty,
        _config["Consultorio:Email"] ?? string.Empty);

    private IQueryable<Pago> QueryBase() => _db.Pagos
        .Include(p => p.Cita).ThenInclude(c => c.Paciente)
        .Include(p => p.Cita).ThenInclude(c => c.Dentista)
        .Include(p => p.Cita).ThenInclude(c => c.CitaTratamientos).ThenInclude(ct => ct.Tratamiento);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PagoDto>>> Get(
        [FromQuery] int? citaId,
        [FromQuery] int? pacienteId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? metodo,
        [FromQuery] string? estado)
    {
        var query = QueryBase();

        if (citaId.HasValue)
            query = query.Where(p => p.CitaId == citaId.Value);
        if (pacienteId.HasValue)
            query = query.Where(p => p.Cita.PacienteId == pacienteId.Value);
        if (desde.HasValue)
            query = query.Where(p => p.FechaPago.Date >= desde.Value.Date.AddDays(-1));
        if (hasta.HasValue)
            query = query.Where(p => p.FechaPago.Date <= hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(metodo) && metodo != "Todos")
            query = query.Where(p => p.MetodoPago == metodo);
        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            query = query.Where(p => p.Estado == estado);

        var pagos = await query.OrderByDescending(p => p.FechaPago).ToListAsync();
        return Ok(pagos.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagoDto>> GetById(int id)
    {
        var pago = await QueryBase().FirstOrDefaultAsync(p => p.PagoId == id);
        return pago is null ? NotFound() : Ok(ToDto(pago));
    }

    [HttpGet("{id:int}/comprobante")]
    public async Task<ActionResult<ComprobanteDto>> GetComprobante(int id)
    {
        var pago = await QueryBase().FirstOrDefaultAsync(p => p.PagoId == id);
        return pago is null ? NotFound() : Ok(ToComprobante(pago));
    }

    [HttpPost]
    public async Task<ActionResult<PagoDto>> Post(CrearPagoDto dto)
    {
        if (!await _db.Citas.AnyAsync(c => c.CitaId == dto.CitaId))
            return BadRequest($"No existe la cita {dto.CitaId}.");

        if (dto.Monto <= 0)
            return BadRequest("Monto debe ser mayor que 0.");

        if (!MetodosValidos.Contains(dto.MetodoPago))
            return BadRequest($"MetodoPago no válido. Usa: {string.Join(", ", MetodosValidos)}.");

        var estado = string.IsNullOrWhiteSpace(dto.Estado) ? "Pagado" : dto.Estado;
        if (!EstadosValidos.Contains(estado))
            return BadRequest($"Estado no válido. Usa: {string.Join(", ", EstadosValidos)}.");

        var pago = new Pago
        {
            CitaId = dto.CitaId,
            Monto = dto.Monto,
            MetodoPago = dto.MetodoPago,
            Estado = estado,
            FechaPago = DateTime.UtcNow,
        };

        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();

        var creado = await QueryBase().FirstAsync(p => p.PagoId == pago.PagoId);
        return CreatedAtAction(nameof(GetById), new { id = pago.PagoId }, ToDto(creado));
    }
}