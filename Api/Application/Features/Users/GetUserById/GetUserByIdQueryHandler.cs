using Api.Domain.Repositories;
using Api.Domain.ValueObjects;
using MediatR;

namespace Api.Application.Features.Users.GetUserById;

/// <summary>
/// Manejador para la consulta GetUserByIdQuery.
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public GetUserByIdQueryHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<GetUserByIdResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Instanciamos el objeto de valor UserId con validación
        var userId = UserId.From(request.Id);

        // 2. Buscamos en el repositorio
        var usuario = await _usuarioRepository.GetByIdAsync(userId, cancellationToken);
        if (usuario is null)
        {
            throw new KeyNotFoundException($"No se encontró ningún usuario con el ID especificado.");
        }

        // 3. Mapeamos y retornamos la respuesta
        return new GetUserByIdResponse(
            usuario.Id.Value,
            usuario.Nombre.Value,
            usuario.Apellido.Value,
            usuario.Email.Value);
    }
}
