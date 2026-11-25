// Controllers/AuthController.cs — Login de administrador (2026-11-20)
// POST   /auth/login            — usuario + contraseña, devuelve el token JWT.
// POST   /auth/cambiar-password — cambia la contraseña del usuario logueado.
// POST   /auth/usuarios         — crea un usuario (solo Admin).
// GET    /auth/usuarios         — lista de usuarios, sin el hash (solo Admin).
// PUT    /auth/usuarios/{id}/activo   — desactiva o reactiva (solo Admin).
// PUT    /auth/usuarios/{id}/password — restablece la contraseña (solo Admin).
//
// 2026-11-21 (bitácora): crear usuario, cambiar su estado y
// restablecer su contraseña quedan registrados en la bitácora.
//
// El token dura las horas de "Jwt:ExpiracionHoras" (8 por defecto)
// y se manda en cada llamada como: Authorization: Bearer <token>.
// Las contraseñas se verifican y se guardan solo como hash
// (PasswordHasher de ASP.NET Core): aquí nunca viaja ni se guarda
// una contraseña en texto, salvo el propio login.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConsultorioDental.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private static readonly string[] RolesValidos = { "Admin", "Recepcionista" };

    private readonly ConsultorioDbContext _db;
    private readonly IConfiguration _config;
    private readonly BitacoraService _bitacora;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthController(ConsultorioDbContext db, IConfiguration config, BitacoraService bitacora)
    {
        _db = db;
        _config = config;
        _bitacora = bitacora;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        if (string.IsNullOrWhiteSpace(login.NombreUsuario) || string.IsNullOrWhiteSpace(login.Password))
            return BadRequest("Usuario y contraseña son obligatorios.");

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == login.NombreUsuario.Trim());
        if (usuario is null || !usuario.Activo)
            return Unauthorized("Usuario o contraseña incorrectos.");

        var valido = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password);
        if (valido == PasswordVerificationResult.Failed)
            return Unauthorized("Usuario o contraseña incorrectos.");

        var expiracionHoras = _config.GetValue<int?>("Jwt:ExpiracionHoras") ?? 8;
        var expiracion = DateTime.UtcNow.AddHours(expiracionHoras);
        var token = GenerarToken(usuario, expiracion);
        return Ok(new LoginRespuestaDto(token, usuario.NombreUsuario, usuario.Rol, expiracion));
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto cambio)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario is null)
            return Unauthorized();

        if (_hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, cambio.PasswordActual)
            == PasswordVerificationResult.Failed)
            return BadRequest("La contraseña actual no es correcta.");

        if (string.IsNullOrWhiteSpace(cambio.PasswordNuevo) || cambio.PasswordNuevo.Length < 8)
            return BadRequest("La contraseña nueva debe tener al menos 8 caracteres.");

        usuario.PasswordHash = _hasher.HashPassword(usuario, cambio.PasswordNuevo);
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("CambiarPassword", "Usuario", usuario.UsuarioId, usuario.NombreUsuario);
        return Ok("Contraseña actualizada.");
    }

    [HttpPost("usuarios")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto nuevo)
    {
        if (string.IsNullOrWhiteSpace(nuevo.NombreUsuario) || string.IsNullOrWhiteSpace(nuevo.Password))
            return BadRequest("Usuario y contraseña son obligatorios.");
        if (nuevo.Password.Length < 8)
            return BadRequest("La contraseña debe tener al menos 8 caracteres.");
        if (!RolesValidos.Contains(nuevo.Rol))
            return BadRequest("El rol debe ser 'Admin' o 'Recepcionista'.");
        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nuevo.NombreUsuario.Trim()))
            return Conflict("Ya existe un usuario con ese nombre.");

        var usuario = new Usuario
        {
            NombreUsuario = nuevo.NombreUsuario.Trim(),
            Rol = nuevo.Rol,
            Activo = true
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, nuevo.Password);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("Crear", "Usuario", usuario.UsuarioId, $"{usuario.NombreUsuario} ({usuario.Rol})");
        return StatusCode(StatusCodes.Status201Created,
            new UsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Rol, usuario.Activo));
    }

    [HttpGet("usuarios")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarUsuarios()
    {
        var usuarios = await _db.Usuarios
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new UsuarioDto(u.UsuarioId, u.NombreUsuario, u.Rol, u.Activo))
            .ToListAsync();
        return Ok(usuarios);
    }

    // PUT /auth/usuarios/{id}/activo — desactiva o reactiva un
    // usuario (solo Admin). No se borra el usuario: se conserva
    // para que la bitácora pueda decir quién hizo cada cambio.
    // Un usuario desactivado ya no puede entrar (login da 401).
    [HttpPut("usuarios/{id:int}/activo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CambiarEstadoUsuario(int id, [FromBody] CambiarEstadoUsuarioDto cambio)
    {
        var usuarioIdActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == usuarioIdActual)
            return BadRequest("No puedes desactivar tu propio usuario.");

        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound("No existe un usuario con ese id.");

        usuario.Activo = cambio.Activo;
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync(usuario.Activo ? "Reactivar" : "Desactivar", "Usuario", usuario.UsuarioId, usuario.NombreUsuario);
        return Ok(new UsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Rol, usuario.Activo));
    }

    // PUT /auth/usuarios/{id}/password — el Admin le pone una
    // contraseña nueva a un usuario (solo Admin). El usuario debe
    // cambiarla en cuanto entre (POST /auth/cambiar-password).
    [HttpPut("usuarios/{id:int}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestablecerPassword(int id, [FromBody] RestablecerPasswordDto cambio)
    {
        if (string.IsNullOrWhiteSpace(cambio.PasswordNuevo) || cambio.PasswordNuevo.Length < 8)
            return BadRequest("La contraseña nueva debe tener al menos 8 caracteres.");

        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound("No existe un usuario con ese id.");

        usuario.PasswordHash = _hasher.HashPassword(usuario, cambio.PasswordNuevo);
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("RestablecerPassword", "Usuario", usuario.UsuarioId, usuario.NombreUsuario);
        return Ok("Contraseña actualizada.");
    }

    private string GenerarToken(Usuario usuario, DateTime expiracion)
    {
        var clave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiracion,
            signingCredentials: credenciales);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
