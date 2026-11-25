// Program.cs : ConsultorioDental.Api (ASP.NET Core 8, Web API)
//
// 2026-11-19: la cadena de conexión ya no vive en los archivos de
// UAT/Staging ni Producción: esos archivos no tienen sección
// ConnectionStrings. La cadena se pone por variable de entorno.
//
// Cómo lo lee ASP.NET Core (sin instalar nada): el proveedor de
// variables de entorno ya viene en CreateBuilder y convierte el
// nombre con doble guion bajo en la jerarquía de configuración, así
// que la variable:
//     ConnectionStrings__DefaultConnection
// llega como Configuration["ConnectionStrings:DefaultConnection"] y
// GetConnectionString("DefaultConnection") la devuelve. Tiene más
// prioridad que cualquier appsettings.json.
//
// Orden de búsqueda de la cadena:
//   1. Variable de entorno ConnectionStrings__DefaultConnection
//      (o, por compatibilidad, ConnectionStrings__SqlConnection).
//   2. appsettings.{Entorno}.json -> ConnectionStrings.
//   3. appsettings.json -> ConnectionStrings.
// Se busca primero "DefaultConnection" (nombre estándar) y luego
// "SqlConnection", que es el nombre viejo que usaba este proyecto.
// Si no hay ninguna, el API no arranca y el mensaje dice dónde
// ponerla, en vez de fallar después con un error de SQL confuso.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var entorno = builder.Environment.EnvironmentName;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("SqlConnection")
    ?? throw new InvalidOperationException(
        $"Falta la cadena de conexión del entorno '{entorno}'. " +
        "Ponla como variable de entorno 'ConnectionStrings__DefaultConnection' " +
        "(ejemplo: Server=TU_SERVIDOR;Database=ConsultorioDental;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True) " +
        "y reinicia el API. En desarrollo local también vale 'dotnet user-secrets'. " +
        "La cadena ya no debe estar en appsettings.Staging.json ni en appsettings.Production.json.");

builder.Services.AddDbContext<ConsultorioDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2026-11-20 (login): autenticación con JWT. La clave se lee de
// "Jwt:Key": en UAT/Producción ponla por variable de entorno
// (Jwt__Key, al menos 32 caracteres); en desarrollo vale la de
// appsettings.json, que es SOLO para desarrollo. Sin clave válida
// el API no arranca, igual que sin cadena de conexión.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Falta la clave JWT ('Jwt:Key'). Ponla por variable de entorno 'Jwt__Key' " +
        "(al menos 32 caracteres) y reinicia el API.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException(
        "La clave JWT ('Jwt:Key') debe tener al menos 32 caracteres.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ConsultorioDental.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ConsultorioDental.App";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// 2026-11-21 (bitácora): acceso al HttpContext (para saber qué
// usuario logueado hizo cada cambio) y el servicio que escribe
// en la tabla Bitacora.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BitacoraService>();

builder.Services.AddControllers();

// CORS abierto solo para desarrollo (probar desde el navegador o
// desde otra app). El HttpClient de MAUI no necesita CORS, pero
// Swagger / fetch desde web sí.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
// 2026-11-20 (login): Swagger con el botón Authorize para el
// token JWT. Flujo: POST /auth/login -> copiar el token ->
// botón Authorize (candado, arriba a la derecha) -> pegar el
// token (sin la palabra Bearer, Swagger la agrega) -> Authorize.
// Después, cada llamada manda Authorization: Bearer <token>.
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token JWT de POST /auth/login. Pega solo el token (sin 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 2026-11-20 (login): si la tabla Usuarios está vacía, crea el
// usuario inicial (admin). Si la base o la tabla no existen aún
// (falta correr el script), solo avisa y el API arranca igual.
try
{
    using var alcance = app.Services.CreateScope();
    await DbSeeder.CrearAdminInicialAsync(alcance.ServiceProvider);
}
catch (Exception ex)
{
    Console.WriteLine($"[Login] No se pudo crear el usuario inicial: {ex.Message}");
    Console.WriteLine("[Login] Ejecuta DB/002-Usuarios.sql y reinicia el API.");
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Staging"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consultorio Dental");
        c.RoutePrefix = string.Empty; // Esto hace que Swagger UI esté en la raíz "/"
    });
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();
app.MapControllers();

app.Run();
