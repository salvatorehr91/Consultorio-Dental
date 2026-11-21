// Data/DbSeeder.cs — Usuario inicial del login (2026-11-20)
// Si la tabla Usuarios está vacía, crea el administrador inicial
// con los datos de "AdminInicial" (appsettings.json, o variables
// de entorno ADMIN_USUARIO / ADMIN_PASSWORD si están puestas).
// La contraseña se guarda solo como hash (PasswordHasher).
// CAMBIA esa contraseña en cuanto entres por primera vez:
// POST /auth/cambiar-password con el token del login.

using ConsultorioDental.Api.Context;
using ConsultorioDental.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Data;

public static class DbSeeder
{
    public static async Task CrearAdminInicialAsync(IServiceProvider servicios)
    {
        var config = servicios.GetRequiredService<IConfiguration>();
        var db = servicios.GetRequiredService<ConsultorioDbContext>();

        if (await db.Usuarios.AnyAsync())
            return;

        var nombre = config["ADMIN_USUARIO"]
            ?? config["AdminInicial:NombreUsuario"]
            ?? "admin";
        var password = config["ADMIN_PASSWORD"]
            ?? config["AdminInicial:Password"]
            ?? "Admin123!";

        var admin = new Usuario
        {
            NombreUsuario = nombre,
            Rol = "Admin",
            Activo = true
        };
        admin.PasswordHash = new PasswordHasher<Usuario>().HashPassword(admin, password);

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Login] Usuario inicial creado: '{nombre}' (rol Admin). Cambia su contraseña en cuanto entres.");
    }
}
