using System.Net;
using Api.Application.Features.Users.CreateUser;
using Api.Application.Features.Users.DeleteUser;
using Api.Application.Features.Users.GetUserById;
using Api.Application.Features.Users.GetUsers;
using Api.Infrastructure.Data;
using Api.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Api.Tests.Features.Commands;

/// <summary>
/// Pruebas funcionales de integración para el comando DeleteUser.
/// </summary>
public class DeleteUserTests : BaseIntegrationTest
{
    [Test]
    public async Task DeleteUser_WithExistingId_ShouldSoftDeleteUser()
    {
        // Arrange: Crear un usuario
        var createCommand = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(createCommand);
        var userId = Api.Domain.ValueObjects.UserId.From(createdUser.Id);

        // Act: Ejecutar soft delete
        var deleteCommand = new DeleteUserCommand(createdUser.Id);
        var success = await Sender.Send(deleteCommand);

        // Assert
        success.ShouldBeTrue();

        // 1. Verificar que GetUserByIdQuery arroje KeyNotFoundException (gracias al filtro global de EF Core)
        var getQuery = new GetUserByIdQuery(createdUser.Id);
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await Sender.Send(getQuery);
        });

        // 2. Verificar que GetUsersQuery no devuelva al usuario
        var listQuery = new GetUsersQuery(Page: 1, PageSize: 10);
        var listResponse = await Sender.Send(listQuery);
        listResponse.Items.Any(u => u.Id == createdUser.Id).ShouldBeFalse();

        // 3. Verificar persistencia física real: el usuario aún existe en la base de datos pero tiene IsDeleted = true
        var db = Services.GetRequiredService<UsuariosDbContext>();
        var userInDb = await db.Usuarios
            .IgnoreQueryFilters() // Ignora el filtro global de soft delete
            .FirstOrDefaultAsync(u => u.Id == userId);

        userInDb.ShouldNotBeNull();
        userInDb.IsDeleted.ShouldBeTrue();
    }

    [Test]
    public async Task DeleteUser_WithNonExistingId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new DeleteUserCommand(Guid.NewGuid());

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await Sender.Send(command);
        });
    }

    [Test]
    public async Task DeleteUser_WithSoftDeletedEmail_ShouldAllowNewRegistrationWithSameEmail()
    {
        // Arrange: Crear y luego eliminar un usuario con email específico
        var targetEmail = "freed.email@example.com";
        var firstCommand = UserTestFactory.CreateValidUserCommand(email: targetEmail);
        var firstUser = await Sender.Send(firstCommand);

        // Eliminarlo
        await Sender.Send(new DeleteUserCommand(firstUser.Id));

        // Act: Intentar registrar un NUEVO usuario con el MISMO correo electrónico
        var secondCommand = UserTestFactory.CreateValidUserCommand(email: targetEmail);
        
        // Assert: No debería lanzar excepción porque el único activo con ese correo fue eliminado
        var secondUser = await Sender.Send(secondCommand);
        secondUser.ShouldNotBeNull();
        secondUser.Id.ShouldNotBe(firstUser.Id);
        secondUser.Email.ShouldBe(targetEmail);
    }

    [Test]
    public async Task DeleteUserEndpoint_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange: Crear un usuario
        var createdUser = await Sender.Send(UserTestFactory.CreateValidUserCommand());

        // Act: Petición DELETE HTTP
        var httpResponse = await HttpClient.DeleteAsync($"/api/usuarios/{createdUser.Id}");

        // Assert
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verificar HTTP GET del mismo recurso retorne 404
        var getResponse = await HttpClient.GetAsync($"/api/usuarios/{createdUser.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteUserEndpoint_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange: ID aleatorio
        var nonExistingId = Guid.NewGuid();

        // Act: Petición DELETE HTTP
        var httpResponse = await HttpClient.DeleteAsync($"/api/usuarios/{nonExistingId}");

        // Assert
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
