using MediatR;

namespace Api.Application.Features.Users.GetUserById;

/// <summary>
/// Consulta para obtener un usuario por su identificador.
/// </summary>
public record GetUserByIdQuery(Guid Id) : IRequest<GetUserByIdResponse>;
