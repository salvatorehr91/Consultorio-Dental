using ConsultorioDental.Api.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Controllers
{
    [ApiController]
    [Route("context/[controller]")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly ConsultorioDbContext _db;
        public DiagnosticoController(ConsultorioDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<object>> Get()
        {
            var conexion = _db.Database.GetDbConnection();
            var tablas = new List<string>();

            await using var cmd = conexion.CreateCommand();
            cmd.CommandText = @"
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME";
            if (conexion.State != System.Data.ConnectionState.Open)
                await conexion.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tablas.Add(reader.GetString(0));

            return Ok(new
            {
                servidor = conexion.DataSource,
                baseDatos = conexion.Database,
                tablasQueVeElApi = tablas,
            });
        }
    }
}
