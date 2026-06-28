using MediatR;

namespace Api.Application.Features.Users.CreateUser;

/// <summary>
/// Comando para crear un nuevo usuario.
/// </summary>
public record CreateUserCommand(
    string Nombre,
    string Apellido,
    string Email) : IRequest<CreateUserResponse>;
