using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

/// <summary>
/// Clase para inicializar y sembrar la base de datos con datos de prueba (Bogus).
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(UsuariosDbContext context)
    {
        // Asegurarse de que la base de datos y las tablas existan
        await context.Database.EnsureCreatedAsync();

        // Evitar doble siembra si ya existen datos
        if (await context.Usuarios.AnyAsync())
        {
            return;
        }

        // Configuración del generador de datos ficticios Bogus
        var userFaker = new Faker<Usuario>("es").CustomInstantiator(f =>
        {
            var id = UserId.New();
            var nombre = Nombre.From(f.Name.FirstName());
            var apellido = Apellido.From(f.Name.LastName());
            var email = Email.From(f.Internet.Email(nombre.Value, apellido.Value));
            return Usuario.Create(id, nombre, apellido, email);
        });

        // Generar 15 usuarios semilla
        var usuariosGenerados = userFaker.Generate(15);

        // Agrupar por correo electrónico (minúsculas) para garantizar unicidad e impedir duplicaciones en la siembra
        var usuariosUnicos = usuariosGenerados
            .GroupBy(u => u.Email.Value.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        await context.Usuarios.AddRangeAsync(usuariosUnicos);
        await context.SaveChangesAsync();
    }
}
