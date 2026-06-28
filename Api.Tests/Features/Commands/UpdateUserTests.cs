using System.Net;
using System.Net.Http.Json;
using Api.Application.Features.Users.CreateUser;
using Api.Application.Features.Users.UpdateUser;
using Api.Infrastructure.Data;
using Api.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Api.Domain.Common;

namespace Api.Tests.Features.Commands;

/// <summary>
/// Pruebas funcionales de integración para el comando UpdateUser.
/// </summary>
public class UpdateUserTests : BaseIntegrationTest
{
    [Test]
    public async Task UpdateUser_WithValidData_ShouldUpdateUser()
    {
        // Arrange: Crear un usuario
        var createCommand = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(createCommand);

        var updateCommand = new UpdateUserCommand(
            Id: createdUser.Id,
            Nombre: "Carlos",
            Apellido: "Gomez",
            Email: "carlos.gomez@example.com"
        );

        // Act
        var response = await Sender.Send(updateCommand);

        // Assert
        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdUser.Id);
        response.Nombre.ShouldBe(updateCommand.Nombre);
        response.Apellido.ShouldBe(updateCommand.Apellido);
        response.Email.ShouldBe(updateCommand.Email);

        // Verificar persistencia física en base de datos
        var db = Services.GetRequiredService<UsuariosDbContext>();
        var userInDb = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == Api.Domain.ValueObjects.UserId.From(createdUser.Id));
        
        userInDb.ShouldNotBeNull();
        userInDb.Nombre.Value.ShouldBe(updateCommand.Nombre);
        userInDb.Apellido.Value.ShouldBe(updateCommand.Apellido);
        userInDb.Email.Value.ShouldBe(updateCommand.Email);
    }

    [Test]
    public async Task UpdateUser_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange: Crear dos usuarios
        var command1 = UserTestFactory.CreateValidUserCommand();
        var user1 = await Sender.Send(command1);

        var command2 = UserTestFactory.CreateValidUserCommand();
        var user2 = await Sender.Send(command2);

        // Intentar actualizar user2 con el email de user1
        var updateCommand = new UpdateUserCommand(
            Id: user2.Id,
            Nombre: user2.Nombre,
            Apellido: user2.Apellido,
            Email: user1.Email // Email duplicado
        );

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await Sender.Send(updateCommand);
        });
    }

    [Test]
    public async Task UpdateUser_WithSameEmail_ShouldSucceed()
    {
        // Arrange: Crear un usuario
        var createCommand = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(createCommand);

        // Actualizar datos del usuario manteniendo el mismo correo
        var updateCommand = new UpdateUserCommand(
            Id: createdUser.Id,
            Nombre: "NuevoNombre",
            Apellido: "NuevoApellido",
            Email: createdUser.Email
        );

        // Act
        var response = await Sender.Send(updateCommand);

        // Assert
        response.ShouldNotBeNull();
        response.Nombre.ShouldBe("NuevoNombre");
        response.Apellido.ShouldBe("NuevoApellido");
        response.Email.ShouldBe(createdUser.Email);
    }

    [Test]
    public async Task UpdateUser_WithInvalidData_ShouldThrowValidationException()
    {
        // Arrange: Crear un usuario
        var createCommand = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(createCommand);

        var updateCommand = new UpdateUserCommand(
            Id: createdUser.Id,
            Nombre: "", // Vacío inválido
            Apellido: "Gomez",
            Email: "correo-invalido" // Formato inválido
        );

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(updateCommand);
        });

        exception.Errors.ContainsKey("Nombre").ShouldBeTrue();
        exception.Errors.ContainsKey("Email").ShouldBeTrue();
    }

    [Test]
    public async Task UpdateUser_WithNonExistingId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var updateCommand = new UpdateUserCommand(
            Id: Guid.NewGuid(),
            Nombre: "Carlos",
            Apellido: "Gomez",
            Email: "carlos.gomez@example.com"
        );

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await Sender.Send(updateCommand);
        });
    }

    [Test]
    public async Task UpdateUserEndpoint_WithValidData_ShouldReturnOkAndUpdatedUser()
    {
        // Arrange: Crear un usuario
        var createCommand = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(createCommand);

        var request = new UpdateUserRequest("Pedro", "Sanz", "pedro.sanz@example.com");

        // Act: Petición PUT
        var httpResponse = await HttpClient.PutAsJsonAsync($"/api/usuarios/{createdUser.Id}", request);

        // Assert
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var userResponse = await httpResponse.Content.ReadFromJsonAsync<UpdateUserResponse>();
        userResponse.ShouldNotBeNull();
        userResponse.Id.ShouldBe(createdUser.Id);
        userResponse.Nombre.ShouldBe(request.Nombre);
        userResponse.Apellido.ShouldBe(request.Apellido);
        userResponse.Email.ShouldBe(request.Email);
    }

    [Test]
    public async Task UpdateUserEndpoint_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange: Crear dos usuarios
        var user1 = await Sender.Send(UserTestFactory.CreateValidUserCommand());
        var user2 = await Sender.Send(UserTestFactory.CreateValidUserCommand());

        var request = new UpdateUserRequest(user2.Nombre, user2.Apellido, user1.Email); // Duplicado

        // Act: Petición PUT
        var httpResponse = await HttpClient.PutAsJsonAsync($"/api/usuarios/{user2.Id}", request);

        // Assert
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
