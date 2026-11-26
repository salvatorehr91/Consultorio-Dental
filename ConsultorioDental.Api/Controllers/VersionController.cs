using ConsultorioDental.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultorioDental.Api.Controllers;

// GET /version — versión actual de la app y enlace del APK
// (2026-11-25, actualización por APK). Sin login: la app lo
// consulta al abrir y con la sesión vencida. La URL del APK es
// pública por diseño. Los valores salen de appsettings:
// "Actualizacion:Version" y "Actualizacion:UrlApk".
[ApiController]
[Route("[controller]")]
public class VersionController : ControllerBase
{
    private readonly IConfiguration _config;

    public VersionController(IConfiguration config) => _config = config;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new VersionAppDto(
        _config["Actualizacion:Version"] ?? "1.0.0",
        _config["Actualizacion:UrlApk"] ?? string.Empty,
        _config["Actualizacion:Notas"]));
}
