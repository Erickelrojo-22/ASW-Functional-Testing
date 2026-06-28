namespace Api.Application.Features.Users.GetUserById;

/// <summary>
/// Respuesta de la consulta para obtener un usuario por su identificador.
/// </summary>
public record GetUserByIdResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email);
