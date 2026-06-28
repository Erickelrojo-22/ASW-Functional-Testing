using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Users.UpdateUser;

/// <summary>
/// Registro y configuración del Endpoint de actualización de usuario.
/// </summary>
public static class UpdateUserEndpoint
{
    public static void MapUpdateUser(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/usuarios/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateUserCommand(id, request.Nombre, request.Apellido, request.Email);
                var response = await sender.Send(command, cancellationToken);
                return Results.Ok(response);
            }
            catch (Domain.Common.ValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = "Regla de negocio violada", Detalle = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = "No encontrado", Detalle = ex.Message });
            }
        })
        .WithName("UpdateUser");
    }
}

// --- DTOs (Mapeos) ---

public record UpdateUserRequest(string Nombre, string Apellido, string Email);

public record UpdateUserResponse(Guid Id, string Nombre, string Apellido, string Email);

// --- CQRS Command ---

public record UpdateUserCommand(Guid Id, string Nombre, string Apellido, string Email) : IRequest<UpdateUserResponse>
{
    /// <summary>
    /// Realiza una validación acumulativa sobre el comando de actualización y lanza una excepción si hay errores.
    /// </summary>
    public static void Validate(UpdateUserCommand command)
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

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly UsuariosDbContext _context;

    public UpdateUserCommandHandler(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Ejecutar validación acumulativa del comando
        UpdateUserCommand.Validate(request);

        // 2. Convertir el Guid en un UserId de dominio
        var userId = UserId.From(request.Id);

        // 3. Buscar el usuario activo (el filtro global excluye automáticamente los eliminados)
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (usuario is null)
        {
            throw new KeyNotFoundException("No se encontró ningún usuario con el ID especificado.");
        }

        // 4. Instanciar los objetos de valor mediante Vogen
        var nuevoNombre = Nombre.From(request.Nombre);
        var nuevoApellido = Apellido.From(request.Apellido);
        var nuevoEmail = Email.From(request.Email);

        // 5. Validación de regla de negocio: si el email cambia, verificar que sea único entre los no eliminados
        if (usuario.Email != nuevoEmail)
        {
            var emailExists = await _context.Usuarios.AnyAsync(u => u.Email == nuevoEmail, cancellationToken);
            if (emailExists)
            {
                throw new InvalidOperationException("El correo electrónico ya está registrado por otro usuario.");
            }
        }

        // 6. Actualizar entidad
        usuario.Update(nuevoNombre, nuevoApellido, nuevoEmail);

        // 7. Persistir
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Retornar respuesta
        return new UpdateUserResponse(
            usuario.Id.Value,
            usuario.Nombre.Value,
            usuario.Apellido.Value,
            usuario.Email.Value);
    }
}
