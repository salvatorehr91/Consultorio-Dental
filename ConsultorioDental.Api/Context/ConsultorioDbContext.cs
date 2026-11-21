using ConsultorioDental.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultorioDental.Api.Context;

public class ConsultorioDbContext : DbContext
{
    public ConsultorioDbContext(DbContextOptions<ConsultorioDbContext> options) : base(options) { }

    public DbSet<Dentista> Dentistas => Set<Dentista>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Tratamiento> Tratamientos => Set<Tratamiento>();
    public DbSet<Cita> Citas => Set<Cita>();

    public DbSet<Cita> Cita => Set<Cita>();

    public DbSet<CitaTratamiento> CitaTratamientos => Set<CitaTratamiento>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // NombreUsuario único: no puede haber dos usuarios "admin".
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Cita>()
            .HasOne(c => c.Paciente)
            .WithMany(p => p.Citas)
            .HasForeignKey(c => c.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cita>()
            .HasOne(c => c.Dentista)
            .WithMany(d => d.Citas)
            .HasForeignKey(c => c.DentistaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cita>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Citas_Estado",
                "Estado IN ('Programada','Confirmada','Completada','Cancelada','NoAsistio')"));

        modelBuilder.Entity<CitaTratamiento>()
            .HasOne(ct => ct.Cita)
            .WithMany(c => c.CitaTratamientos)
            .HasForeignKey(ct => ct.CitaId);

        modelBuilder.Entity<CitaTratamiento>()
            .HasOne(ct => ct.Tratamiento)
            .WithMany(t => t.CitaTratamientos)
            .HasForeignKey(ct => ct.TratamientoId);

        modelBuilder.Entity<CitaTratamiento>()
            .HasIndex(ct => new { ct.CitaId, ct.TratamientoId })
            .IsUnique();

        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Cita)
            .WithMany(c => c.Pagos)
            .HasForeignKey(p => p.CitaId);

        modelBuilder.Entity<Pago>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Pagos_MetodoPago",
                "MetodoPago IN ('Efectivo','Tarjeta','Transferencia','Otro')"));
    }
}
