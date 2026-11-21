// Controllers/ReportesController.cs — Reportes del consultorio
// GET    /reportes/resumen?desde=2026-11-01&hasta=2026-11-30
//        — total cobrado, cantidad de pagos, promedio por pago,
//        total de citas y citas por estado en el período.
// GET    /reportes/cobros-por-dia?desde=&hasta=
//        — total cobrado y cantidad de pagos por día (incluye los
//        días sin cobros, en 0, para que la serie se lea completa).
// GET    /reportes/tratamientos-mas-usados?desde=&hasta=&top=10
//        — tratamientos ordenados por veces realizados, con el total
//        cobrado por cada uno (precio de CitaTratamientos, el que se
//        cobró en su momento, no el precio actual del catálogo).
// GET    /reportes/citas-por-dentista?desde=&hasta=
//        — citas por dentista en el período, con completadas y
//        canceladas.
//
// Fechas: Desde/Hasta llegan como fecha local del consultorio.
// Las citas guardan FechaHora en hora local, así que se comparan
// directo. Los pagos guardan FechaPago en UTC (DateTime.UtcNow en
// el POST), así que el rango se convierte a UTC con la zona de
// México y el día de cada pago se agrupa en hora de México; si no,
// un pago de las 20:00 en CDMX caería en el día siguiente.
// Sin desde/hasta, el período es el mes en curso (hora de México).
// El rango se limita a 366 días para no devolver series enormes.

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
public class ReportesController : ControllerBase
{
    private readonly ConsultorioDbContext _db;
    public ReportesController(ConsultorioDbContext db) => _db = db;

    // Zona horaria del consultorio (misma idea que HoyEnMexico() en
    // CitasController): "Central Standard Time" en Windows,
    // "America/Mexico_City" en Linux.
    private static TimeZoneInfo ZonaMexico()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
    }

    private static DateTime AFechaMexico(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaMexico()).Date;

    // Período por defecto: mes en curso en hora de México. Devuelve
    // (inicio, fin) como fechas locales (00:00 y 23:59:59 del día).
    private static (DateTime Desde, DateTime Hasta) ResolverPeriodo(DateTime? desde, DateTime? hasta)
    {
        var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaMexico()).Date;
        var d = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var h = (hasta ?? hoy).Date;
        return (d, h);
    }

    private static IActionResult? ValidarPeriodo(DateTime desde, DateTime hasta)
    {
        if (desde > hasta)
            return new BadRequestObjectResult("La fecha Desde no puede ser mayor que Hasta.");
        if ((hasta - desde).TotalDays > 366)
            return new BadRequestObjectResult("El período no puede ser mayor a 366 días.");
        return null;
    }

    // Citas en el período: FechaHora es hora local del consultorio.
    private IQueryable<Cita> CitasEnPeriodo(DateTime desde, DateTime hasta) =>
        _db.Citas.Where(c => c.FechaHora >= desde && c.FechaHora < hasta.AddDays(1));

    // Pagos en el período: FechaPago es UTC. El rango local se pasa
    // a UTC (inicio del día Desde en México -> UTC, fin del día
    // Hasta en México -> UTC) y se amplía 1 día a cada lado por si
    // el servidor corre en otra zona; el día exacto se decide en
    // memoria con AFechaMexico, igual que en PagosController.
    private async Task<List<Pago>> PagosEnPeriodoAsync(DateTime desde, DateTime hasta)
    {
        var zona = ZonaMexico();
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(desde, zona).AddDays(-1);
        var finUtc = TimeZoneInfo.ConvertTimeToUtc(hasta.AddDays(1), zona).AddDays(1);
        var pagos = await _db.Pagos
            .Where(p => p.FechaPago >= inicioUtc && p.FechaPago < finUtc)
            .ToListAsync();
        return pagos
            .Where(p => { var dia = AFechaMexico(p.FechaPago); return dia >= desde && dia <= hasta; })
            .ToList();
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = ResolverPeriodo(desde, hasta);
        var error = ValidarPeriodo(d, h);
        if (error is not null) return error;

        // Solo Estado "Pagado" cuenta como cobrado: Pendiente no se
        // cobró todavía y Reembolsado se devolvió.
        var pagos = await PagosEnPeriodoAsync(d, h);
        var pagados = pagos.Where(p => p.Estado == "Pagado").ToList();
        var totalCobrado = pagados.Sum(p => p.Monto);

        // 2026-11-20: las citas se traen planas (solo el Estado) y
        // se agrupan en memoria. El GroupBy en el servidor no se
        // tradujo en este EF Core (InvalidOperationException), y son
        // pocas filas en un período, así que no pesa.
        var estados = await CitasEnPeriodo(d, h)
            .Select(c => c.Estado)
            .ToListAsync();
        var citasPorEstado = estados
            .GroupBy(e => e)
            .Select(g => new EstadoCantidadDto(g.Key, g.Count()))
            .OrderByDescending(e => e.Cantidad)
            .ToList();
        var totalCitas = citasPorEstado.Sum(e => e.Cantidad);

        return Ok(new ResumenReporteDto(
            totalCobrado,
            pagados.Count,
            pagados.Count == 0 ? 0 : totalCobrado / pagados.Count,
            totalCitas,
            citasPorEstado));
    }

    [HttpGet("cobros-por-dia")]
    public async Task<IActionResult> CobrosPorDia([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = ResolverPeriodo(desde, hasta);
        var error = ValidarPeriodo(d, h);
        if (error is not null) return error;

        var porDia = (await PagosEnPeriodoAsync(d, h))
            .Where(p => p.Estado == "Pagado")
            .GroupBy(p => AFechaMexico(p.FechaPago))
            .ToDictionary(g => g.Key, g => (Total: g.Sum(p => p.Monto), Cantidad: g.Count()));

        // Serie completa del período, con 0 en los días sin cobros.
        var serie = new List<CobroPorDiaDto>();
        for (var dia = d; dia <= h; dia = dia.AddDays(1))
        {
            porDia.TryGetValue(dia, out var valores);
            serie.Add(new CobroPorDiaDto(dia, valores.Total, valores.Cantidad));
        }
        return Ok(serie);
    }

    [HttpGet("tratamientos-mas-usados")]
    public async Task<IActionResult> TratamientosMasUsados(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int top = 10)
    {
        var (d, h) = ResolverPeriodo(desde, hasta);
        var error = ValidarPeriodo(d, h);
        if (error is not null) return error;
        if (top < 1) top = 1;
        if (top > 50) top = 50;

        // 2026-11-20: igual que Resumen, la agrupación se hace en
        // memoria: el GroupBy con Include no se tradujo en el
        // servidor. Se trae solo lo necesario (sin Include).
        var filas = await _db.CitaTratamientos
            .Where(ct => ct.Cita.FechaHora >= d && ct.Cita.FechaHora < h.AddDays(1))
            .Select(ct => new { ct.TratamientoId, Nombre = ct.Tratamiento.Nombre, ct.Cantidad, ct.Precio })
            .ToListAsync();
        var usos = filas
            .GroupBy(x => new { x.TratamientoId, x.Nombre })
            .Select(g => new TratamientoUsoDto(
                g.Key.TratamientoId,
                g.Key.Nombre,
                g.Sum(x => x.Cantidad),
                g.Sum(x => x.Precio * x.Cantidad)))
            .OrderByDescending(t => t.Veces)
            .ThenByDescending(t => t.TotalCobrado)
            .Take(top)
            .ToList();
        return Ok(usos);
    }

    [HttpGet("citas-por-dentista")]
    public async Task<IActionResult> CitasPorDentista([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = ResolverPeriodo(desde, hasta);
        var error = ValidarPeriodo(d, h);
        if (error is not null) return error;

        // 2026-11-20: agrupación en memoria, igual que los otros
        // dos endpoints. La proyección con el nombre del dentista
        // sí se traduce (es un JOIN), pero el GroupBy no.
        var filas = await CitasEnPeriodo(d, h)
            .Select(c => new { c.DentistaId, Nombre = c.Dentista.Nombre + " " + c.Dentista.Apellido, c.Estado })
            .ToListAsync();
        var porDentista = filas
            .GroupBy(x => new { x.DentistaId, x.Nombre })
            .Select(g => new CitasPorDentistaDto(
                g.Key.DentistaId,
                g.Key.Nombre.Trim(),
                g.Count(),
                g.Count(x => x.Estado == "Completada"),
                g.Count(x => x.Estado == "Cancelada")))
            .OrderByDescending(x => x.TotalCitas)
            .ToList();
        return Ok(porDentista);
    }
}
