// Program.cs : ConsultorioDental.Api (ASP.NET Core 8, Web API)

using ConsultorioDental.Api.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ConsultorioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

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
