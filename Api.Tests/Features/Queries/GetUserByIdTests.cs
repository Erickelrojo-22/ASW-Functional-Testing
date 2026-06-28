using System.Net;
using System.Net.Http.Json;
using Api.Application.Features.Users.GetUserById;
using Api.Tests.Factories;
using NUnit.Framework;
using Shouldly;

namespace Api.Tests.Features.Queries;

/// <summary>
/// Pruebas funcionales de integración para la consulta GetUserById.
/// </summary>
public class GetUserByIdTests : BaseIntegrationTest
{
    [Test]
    public async Task GetUserById_WithExistingId_ShouldReturnUser()
    {
        // Arrange
        var command = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(command);

        var query = new GetUserByIdQuery(createdUser.Id);

        // Act
        var response = await Sender.Send(query);

        // Assert
        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdUser.Id);
        response.Nombre.ShouldBe(command.Nombre);
        response.Email.ShouldBe(command.Email);
    }

    [Test]
    public async Task GetUserById_WithNonExistingId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var query = new GetUserByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await Sender.Send(query);
        });
    }

    [Test]
    public async Task GetUserById_WithEmptyGuid_ShouldThrowKeyNotFoundException()
    {
        // Arrange: ID vacío (Guid.Empty)
        var query = new GetUserByIdQuery(Guid.Empty);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await Sender.Send(query);
        });
    }

    [Test]
    public async Task GetUserByIdEndpoint_WithExistingId_ShouldReturnOkAndUser()
    {
        // Arrange: Crear un usuario usando MediatR en la base de datos
        var command = UserTestFactory.CreateValidUserCommand();
        var createdUser = await Sender.Send(command);

        // Act: Hacer la solicitud HTTP al endpoint
        var httpResponse = await HttpClient.GetAsync($"/api/usuarios/{createdUser.Id}");

        // Assert
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var userResponse = await httpResponse.Content.ReadFromJsonAsync<GetUserByIdResponse>();
        userResponse.ShouldNotBeNull();
        userResponse.Id.ShouldBe(createdUser.Id);
        userResponse.Nombre.ShouldBe(command.Nombre);
        userResponse.Email.ShouldBe(command.Email);
    }

    [Test]
    public async Task GetUserByIdEndpoint_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange: Generar un ID aleatorio no existente
        var nonExistingId = Guid.NewGuid();

        // Act: Hacer la solicitud HTTP al endpoint
        var httpResponse = await HttpClient.GetAsync($"/api/usuarios/{nonExistingId}");

        // Assert: Debería retornar 404 Not Found
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUserByIdEndpoint_WithEmptyGuid_ShouldReturnNotFound()
    {
        // Arrange: ID vacío (Guid.Empty)
        var emptyId = Guid.Empty;

        // Act: Hacer la solicitud HTTP al endpoint
        var httpResponse = await HttpClient.GetAsync($"/api/usuarios/{emptyId}");

        // Assert: Debería retornar 404 Not Found
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
