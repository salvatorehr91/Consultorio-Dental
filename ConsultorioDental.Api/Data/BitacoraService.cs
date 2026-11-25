// Data/BitacoraService.cs — Escribe en la bitácora (2026-11-21)
// Los controladores lo llaman DESPUÉS de guardar el cambio, para
// que la bitácora solo registre lo que sí se guardó. El usuario se
// saca del token JWT (claims NameIdentifier y Name): si no hay
// usuario logueado (p. ej. el seeder al arrancar), no registra nada.
// Si el registro en bitácora falla, no rompe la operación que ya
// se guardó: se traga el error y lo escribe en consola.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ConsultorioDental.Api.Data;

public class BitacoraService
{
    private readonly ConsultorioDbContext _db;
    private readonly IHttpContextAccessor _http;

    public BitacoraService(ConsultorioDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task RegistrarAsync(string accion, string entidad, int entidadId, string detalle)
    {
        try
        {
            var usuarioId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nombre = _http.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(usuarioId, out var id) || string.IsNullOrWhiteSpace(nombre))
                return;

            _db.Bitacora.Add(new Bitacora
            {
                FechaHora = DateTime.UtcNow,
                UsuarioId = id,
                UsuarioNombre = nombre,
                Accion = accion,
                Entidad = entidad,
                EntidadId = entidadId,
                Detalle = detalle
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bitácora] No se pudo registrar {accion} {entidad} {entidadId}: {ex.Message}");
        }
    }
}
