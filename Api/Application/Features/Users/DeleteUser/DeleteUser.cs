using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Users.DeleteUser;

/// <summary>
/// Registro y configuración del Endpoint de eliminación soft delete de usuario.
/// </summary>
public static class DeleteUserEndpoint
{
    public static void MapDeleteUser(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/usuarios/{id:guid}", async (
            Guid id,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeleteUserCommand(id);
                var success = await sender.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = "No encontrado", Detalle = ex.Message });
            }
        })
        .WithName("DeleteUser");
    }
}

// --- CQRS Command ---

public record DeleteUserCommand(Guid Id) : IRequest<bool>;

// --- Handler (CQRS) ---

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly UsuariosDbContext _context;

    public DeleteUserCommandHandler(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Convertir el Guid en un UserId de dominio
        var userId = UserId.From(request.Id);

        // 2. Buscar el usuario activo (el filtro global excluye automáticamente los eliminados)
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (usuario is null)
        {
            throw new KeyNotFoundException("No se encontró ningún usuario con el ID especificado.");
        }

        // 3. Aplicar Soft Delete
        usuario.SoftDelete();

        // 4. Persistir los cambios
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
