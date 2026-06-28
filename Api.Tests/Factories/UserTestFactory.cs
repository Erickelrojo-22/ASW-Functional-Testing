using Api.Application.Features.Users.CreateUser;
using Bogus;

namespace Api.Tests.Factories;

/// <summary>
/// Fábrica de datos de prueba para generar comandos y entidades de Usuarios usando Bogus de forma aislada.
/// </summary>
public static class UserTestFactory
{
    private static readonly Faker Faker = new("es");

    /// <summary>
    /// Genera un comando CreateUserCommand con datos realistas en español.
    /// </summary>
    /// <param name="email">Opcional. Si se pasa, sobrescribe el email aleatorio (útil para pruebas de duplicados).</param>
    public static CreateUserCommand CreateValidUserCommand(string? email = null)
    {
        var nombre = Faker.Name.FirstName();
        var apellido = Faker.Name.LastName();
        var correo = email ?? Faker.Internet.Email(nombre, apellido);

        return new CreateUserCommand(nombre, apellido, correo);
    }

    /// <summary>
    /// Genera una lista de comandos de creación de usuario.
    /// </summary>
    public static IEnumerable<CreateUserCommand> CreateManyValidUserCommands(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateValidUserCommand();
        }
    }
}
