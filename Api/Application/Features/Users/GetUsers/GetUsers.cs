using Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Users.GetUsers;

/// <summary>
/// Registro y configuración del Endpoint para obtener la lista paginada de usuarios.
/// </summary>
public static class GetUsersEndpoint
{
    public static void MapGetUsers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/usuarios", async (
            [AsParameters] GetUsersRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                var query = new GetUsersQuery(request.Page, request.PageSize);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            }
            catch (Domain.Common.ValidationException ex)
            {
                // Retorna 400 Bad Request estructurado con los errores de paginación
                return Results.ValidationProblem(ex.Errors);
            }
        })
        .WithName("GetUsers");
    }
}

// --- DTOs (Mapeos) ---

public record GetUsersRequest(int Page = 1, int PageSize = 10);

public record UserDto(Guid Id, string Nombre, string Apellido, string Email);

public record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

// --- CQRS Query ---

public record GetUsersQuery(int Page, int PageSize) : IRequest<PaginatedResponse<UserDto>>
{
    /// <summary>
    /// Realiza una validación acumulativa sobre la consulta y lanza una excepción si hay errores.
    /// </summary>
    public static void Validate(GetUsersQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
        {
            errors.Add(nameof(query.Page), new[] { "El número de página ('page') debe ser mayor o igual a 1." });
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add(nameof(query.PageSize), new[] { "El tamaño de página ('pageSize') debe estar entre 1 y 100." });
        }

        if (errors.Any())
        {
            throw new Domain.Common.ValidationException(errors);
        }
    }
}

// --- Handler (CQRS) ---

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResponse<UserDto>>
{
    private readonly UsuariosDbContext _context;

    public GetUsersQueryHandler(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // 1. Ejecutamos la validación acumulativa de la consulta
        GetUsersQuery.Validate(request);

        // 2. Obtener la consulta de base de datos
        var query = _context.Usuarios.AsNoTracking();

        // 3. Contar total de registros
        var totalCount = await query.CountAsync(cancellationToken);

        // 4. Obtener los ítems de la página actual
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id.Value,
                u.Nombre.Value,
                u.Apellido.Value,
                u.Email.Value))
            .ToListAsync(cancellationToken);

        // 5. Calcular metadatos de paginación
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var hasPreviousPage = request.Page > 1;
        var hasNextPage = request.Page < totalPages;

        return new PaginatedResponse<UserDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages,
            hasPreviousPage,
            hasNextPage);
    }
}
