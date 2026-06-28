using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Users.GetUserById;

/// <summary>
/// Registro y configuración del Endpoint de consulta de usuario por identificador.
/// </summary>
public static class GetUserByIdEndpoint
{
    public static void MapGetUserById(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/usuarios/{id:guid}",
                async (
                    Guid id,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        var query = new GetUserByIdQuery(id);
                        var response = await sender.Send(query, cancellationToken);
                        return Results.Ok(response);
                    }
                    catch (Vogen.ValueObjectValidationException ex)
                    {
                        return Results.BadRequest(
                            new { Error = "Identificador inválido", Detalle = ex.Message }
                        );
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(
                            new { Error = "No encontrado", Detalle = ex.Message }
                        );
                    }
                }
            )
            .WithName("GetUserById");
    }
}

// --- DTOs (Mapeos) ---

public record GetUserByIdResponse(Guid Id, string Nombre, string Apellido, string Email);

// --- CQRS Query ---

public record GetUserByIdQuery(Guid Id) : IRequest<GetUserByIdResponse>;

// --- Handler (CQRS) ---

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    private readonly UsuariosDbContext _context;

    public GetUserByIdQueryHandler(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<GetUserByIdResponse> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        // 1. Instanciamos el objeto de valor UserId con validación
        var userId = UserId.From(request.Id);

        // 2. Buscamos directamente en el DbContext
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(
            u => u.Id == userId,
            cancellationToken
        );
        if (usuario is null)
        {
            throw new KeyNotFoundException(
                $"No se encontró ningún usuario con el ID especificado."
            );
        }

        // 3. Mapeamos y retornamos la respuesta
        return new GetUserByIdResponse(
            usuario.Id.Value,
            usuario.Nombre.Value,
            usuario.Apellido.Value,
            usuario.Email.Value
        );
    }
}
