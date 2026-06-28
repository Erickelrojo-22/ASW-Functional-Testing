using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Users.CreateUser;

/// <summary>
/// Registro y configuración del Endpoint de creación de usuario.
/// </summary>
public static class CreateUserEndpoint
{
    public static void MapCreateUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/usuarios", async (
            [FromBody] CreateUserRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new CreateUserCommand(request.Nombre, request.Apellido, request.Email);
                var response = await sender.Send(command, cancellationToken);
                return Results.Created($"/api/usuarios/{response.Id}", response);
            }
            catch (Domain.Common.ValidationException ex)
            {
                // Retorna 400 Bad Request estructurado con los errores de validación acumulados
                return Results.ValidationProblem(ex.Errors);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = "Regla de negocio violada", Detalle = ex.Message });
            }
        })
        .WithName("CreateUser");
    }
}

// --- DTOs (Mapeos) ---

public record CreateUserRequest(string Nombre, string Apellido, string Email);

public record CreateUserResponse(Guid Id, string Nombre, string Apellido, string Email);

// --- CQRS Command ---

public record CreateUserCommand(string Nombre, string Apellido, string Email) : IRequest<CreateUserResponse>
{
    /// <summary>
    /// Realiza una validación acumulativa sobre el comando de creación y lanza una excepción si hay errores.
    /// </summary>
    public static void Validate(CreateUserCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        var nombreResult = Api.Domain.ValueObjects.Nombre.TryFrom(command.Nombre);
        if (!nombreResult.IsSuccess)
        {
            errors.Add(nameof(command.Nombre), new[] { nombreResult.Error.ErrorMessage });
        }

        var apellidoResult = Api.Domain.ValueObjects.Apellido.TryFrom(command.Apellido);
        if (!apellidoResult.IsSuccess)
        {
            errors.Add(nameof(command.Apellido), new[] { apellidoResult.Error.ErrorMessage });
        }

        var emailResult = Api.Domain.ValueObjects.Email.TryFrom(command.Email);
        if (!emailResult.IsSuccess)
        {
            errors.Add(nameof(command.Email), new[] { emailResult.Error.ErrorMessage });
        }

        if (errors.Any())
        {
            throw new Domain.Common.ValidationException(errors);
        }
    }
}

// --- Handler (CQRS) ---

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly UsuariosDbContext _context;

    public CreateUserCommandHandler(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Ejecutamos la validación acumulativa estática del comando
        CreateUserCommand.Validate(request);

        // 2. Instanciamos los objetos de valor mediante Vogen
        var nombre = Nombre.From(request.Nombre);
        var apellido = Apellido.From(request.Apellido);
        var email = Email.From(request.Email);

        // 3. Validación de regla de negocio: correo electrónico único
        var emailExists = await _context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("El correo electrónico ya está registrado por otro usuario.");
        }

        // 4. Creación de la entidad
        var userId = UserId.New();
        var usuario = Usuario.Create(userId, nombre, apellido, email);

        // 5. Persistencia directa en base de datos
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Retornamos la respuesta
        return new CreateUserResponse(
            usuario.Id.Value,
            usuario.Nombre.Value,
            usuario.Apellido.Value,
            usuario.Email.Value);
    }
}
