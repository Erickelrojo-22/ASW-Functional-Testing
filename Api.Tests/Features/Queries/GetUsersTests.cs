using Api.Application.Features.Users.GetUsers;
using Api.Tests.Factories;
using NUnit.Framework;
using Shouldly;
using Api.Domain.Common;

namespace Api.Tests.Features.Queries;

/// <summary>
/// Pruebas funcionales de integración para la consulta GetUsers.
/// </summary>
public class GetUsersTests : BaseIntegrationTest
{
    [Test]
    public async Task GetUsers_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        var commands = UserTestFactory.CreateManyValidUserCommands(5).ToList();
        foreach (var cmd in commands)
        {
            await Sender.Send(cmd);
        }

        var query = new GetUsersQuery(Page: 1, PageSize: 3);

        // Act
        var response = await Sender.Send(query);

        // Assert
        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(3);
        response.TotalCount.ShouldBe(5);
        response.TotalPages.ShouldBe(2);
        response.HasNextPage.ShouldBeTrue();
        response.HasPreviousPage.ShouldBeFalse();
    }

    [Test]
    public async Task GetUsers_WithInvalidPageSize_ShouldThrowValidationException()
    {
        // Arrange
        var query = new GetUsersQuery(Page: 1, PageSize: 150);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(query);
        });

        exception.Errors.ContainsKey("PageSize").ShouldBeTrue();
    }

    [Test]
    [TestCase(0)]
    [TestCase(-5)]
    public async Task GetUsers_WithZeroOrNegativePage_ShouldThrowValidationException(int invalidPage)
    {
        // Arrange
        var query = new GetUsersQuery(Page: invalidPage, PageSize: 10);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(query);
        });

        exception.Errors.ContainsKey("Page").ShouldBeTrue();
    }

    [Test]
    public async Task GetUsers_WithPageExceedingTotalPages_ShouldReturnEmptyList()
    {
        // Arrange: Crear 5 usuarios
        var commands = UserTestFactory.CreateManyValidUserCommands(5).ToList();
        foreach (var cmd in commands)
        {
            await Sender.Send(cmd);
        }

        // Consultar página 10 con tamaño 2 (total 3 páginas)
        var query = new GetUsersQuery(Page: 10, PageSize: 2);

        // Act
        var response = await Sender.Send(query);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldBeEmpty();
        response.TotalCount.ShouldBe(5);
        response.TotalPages.ShouldBe(3);
        response.HasNextPage.ShouldBeFalse();
        response.HasPreviousPage.ShouldBeTrue();
    }

    [Test]
    public async Task GetUsers_WhenDatabaseIsEmpty_ShouldReturnEmptyPaginatedResponse()
    {
        // Arrange: Base de datos limpia (garantizado por BaseIntegrationTest)
        var query = new GetUsersQuery(Page: 1, PageSize: 10);

        // Act
        var response = await Sender.Send(query);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
        response.TotalPages.ShouldBe(0);
        response.HasNextPage.ShouldBeFalse();
        response.HasPreviousPage.ShouldBeFalse();
    }
}
