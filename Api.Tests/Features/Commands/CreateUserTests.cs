using Api.Application.Features.Users.CreateUser;
using Api.Domain.Common;
using Api.Infrastructure.Data;
using Api.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Api.Tests.Features.Commands;

/// <summary>
/// Pruebas funcionales de integración para el comando CreateUser.
/// </summary>
public class CreateUserTests : BaseIntegrationTest
{
    [Test]
    public async Task CreateUser_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var command = UserTestFactory.CreateValidUserCommand();

        // Act
        var response = await Sender.Send(command);

        // Assert
        response.ShouldNotBeNull();
        response.Nombre.ShouldBe(command.Nombre);
        response.Apellido.ShouldBe(command.Apellido);
        response.Email.ShouldBe(command.Email);

        // Verificar la persistencia física en la base de datos real
        var db = Services.GetRequiredService<UsuariosDbContext>();
        var userInDb = await db.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == Api.Domain.ValueObjects.Email.From(command.Email)
        );

        userInDb.ShouldNotBeNull();
        userInDb.Nombre.Value.ShouldBe(command.Nombre);
        userInDb.Apellido.Value.ShouldBe(command.Apellido);
    }

    [Test]
    public async Task CreateUser_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var firstCommand = UserTestFactory.CreateValidUserCommand();
        await Sender.Send(firstCommand);

        var duplicateCommand = UserTestFactory.CreateValidUserCommand(email: firstCommand.Email);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await Sender.Send(duplicateCommand);
        });
    }

    [Test]
    public async Task CreateUser_WithInvalidEmail_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateUserCommand("Marcelo", "Gomez", "not-an-email");

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(command);
        });

        exception.Errors.ContainsKey("Email").ShouldBeTrue();
    }

    [Test]
    public async Task CreateUser_WithMultipleInvalidFields_ShouldThrowValidationExceptionWithCumulativeErrors()
    {
        // Arrange
        var command = new CreateUserCommand(Nombre: "   ", Apellido: "", Email: "correo-invalido");

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(command);
        });

        exception.Errors.ContainsKey("Nombre").ShouldBeTrue();
        exception.Errors.ContainsKey("Apellido").ShouldBeTrue();
        exception.Errors.ContainsKey("Email").ShouldBeTrue();
    }

    [Test]
    [TestCase("duplicated@example.com", "DUPLICATED@example.com")]
    [TestCase("duplicated@example.com", "duplicated@example.com")]
    public async Task CreateUser_WithDuplicateEmailDifferentCasing_ShouldThrowInvalidOperationException(
        string email1,
        string email2
    )
    {
        // Arrange: Crear un usuario con el primer email
        var command1 = UserTestFactory.CreateValidUserCommand(email: email1);
        await Sender.Send(command1);

        // Act & Assert: Intentar crear con el segundo email (debería fallar por duplicado, insensible a mayúsculas)
        var command2 = UserTestFactory.CreateValidUserCommand(email: email2);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await Sender.Send(command2);
        });
    }

    [Test]
    public async Task CreateUser_WithNameExactlyAtMaxLength_ShouldSucceed()
    {
        // Arrange: Nombre con exactamente 100 caracteres
        var longName = new string('A', 100);
        var command = new CreateUserCommand(
            Nombre: longName,
            Apellido: "Gomez",
            Email: "max.length@example.com"
        );

        // Act
        var response = await Sender.Send(command);

        // Assert
        response.ShouldNotBeNull();
        response.Nombre.ShouldBe(longName);
        response.Nombre.Length.ShouldBe(100);
    }

    [Test]
    public async Task CreateUser_WithNameExceedingMaxLength_ShouldThrowException()
    {
        // Arrange: Nombre con 101 caracteres (excede HasMaxLength(100))
        var tooLongName = new string('A', 101);
        var command = new CreateUserCommand(
            Nombre: tooLongName,
            Apellido: "Gomez",
            Email: "exceed.length@example.com"
        );

        // Act & Assert: Debería fallar al intentar guardar en la base de datos
        await Should.ThrowAsync<Exception>(async () =>
        {
            await Sender.Send(command);
        });
    }
}
