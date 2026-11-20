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
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

builder.Services.AddControllers();

// CORS abierto solo para desarrollo (probar desde el navegador o
// desde otra app). El HttpClient de MAUI no necesita CORS, pero
// Swagger / fetch desde web sí.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
