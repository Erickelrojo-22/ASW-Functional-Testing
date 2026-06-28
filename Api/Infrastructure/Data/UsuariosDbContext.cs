using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

/// <summary>
/// DbContext de EF Core para la gestión de Usuarios.
/// </summary>
public class UsuariosDbContext : DbContext
{
    public UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("Usuarios");

            // Configuración de la clave primaria (UserId)
            b.HasKey(u => u.Id);

            b.Property(u => u.Id)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired();

            // Configuración de los Objetos de Valor de Vogen
            b.Property(u => u.Nombre)
                .HasConversion(
                    n => n.Value,
                    value => Nombre.From(value))
                .HasMaxLength(100)
                .IsRequired();

            b.Property(u => u.Apellido)
                .HasConversion(
                    a => a.Value,
                    value => Apellido.From(value))
                .HasMaxLength(100)
                .IsRequired();

            b.Property(u => u.Email)
                .HasConversion(
                    e => e.Value,
                    value => Email.From(value))
                .HasMaxLength(150)
                .IsRequired();

            // Índice único en el campo Email
            b.HasIndex(u => u.Email).IsUnique();
        });
    }
}
