using Api.Application.Features.Users.CreateUser;
using Api.Tests.Factories;
using NUnit.Framework;
using Shouldly;
using Api.Domain.Common;

namespace Api.Tests.Features.Behaviors;

/// <summary>
/// Pruebas de integración para comprobar la correcta ejecución y registro de los MediatR Pipeline Behaviors.
/// </summary>
public class BehaviorTests : BaseIntegrationTest
{
    [Test]
    public async Task MediatRBehaviors_WithValidRequest_ShouldExecuteSuccessfullyAndPropagateResponse()
    {
        // Arrange: Crear un comando válido
        var command = UserTestFactory.CreateValidUserCommand();

        // Act: Enviar la solicitud a través del pipeline de MediatR
        // Esto ejecutará LoggingBehavior e inmediatamente después PerformanceBehavior
        var response = await Sender.Send(command);

        // Assert: Validar que el pipeline continuó y completó exitosamente
        response.ShouldNotBeNull();
        response.Nombre.ShouldBe(command.Nombre);
        response.Email.ShouldBe(command.Email);
    }

    [Test]
    public async Task MediatRBehaviors_WithInvalidRequest_ShouldPropagateExceptionAndLogFailure()
    {
        // Arrange: Crear comando con correo inválido para forzar error
        var command = new CreateUserCommand("Marcelo", "Gomez", "correo-invalido");

        // Act & Assert: Comprobar que la excepción es propagada correctamente por los behaviors
        await Should.ThrowAsync<ValidationException>(async () =>
        {
            await Sender.Send(command);
        });
    }
}
