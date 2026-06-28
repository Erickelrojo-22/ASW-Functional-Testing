namespace Api.Application.Features.Users.CreateUser;

/// <summary>
/// Respuesta del comando de creación de usuario.
/// </summary>
public record CreateUserResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email);
