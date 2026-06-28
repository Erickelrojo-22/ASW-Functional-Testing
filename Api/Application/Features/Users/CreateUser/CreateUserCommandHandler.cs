using Api.Domain.Entities;
using Api.Domain.Repositories;
using Api.Domain.ValueObjects;
using MediatR;

namespace Api.Application.Features.Users.CreateUser;

/// <summary>
/// Manejador para el comando CreateUserCommand.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public CreateUserCommandHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Instanciamos y validamos los objetos de valor mediante Vogen
        var nombre = Nombre.From(request.Nombre);
        var apellido = Apellido.From(request.Apellido);
        var email = Email.From(request.Email);

        // 2. Validación de regla de negocio: correo electrónico único
        var isUnique = await _usuarioRepository.IsEmailUniqueAsync(email, cancellationToken);
        if (!isUnique)
        {
            throw new InvalidOperationException("El correo electrónico ya está registrado por otro usuario.");
        }

        // 3. Creación de la entidad
        var userId = UserId.New();
        var usuario = Usuario.Create(userId, nombre, apellido, email);

        // 4. Persistencia
        await _usuarioRepository.AddAsync(usuario, cancellationToken);

        // 5. Retornamos la respuesta
        return new CreateUserResponse(
            usuario.Id.Value,
            usuario.Nombre.Value,
            usuario.Apellido.Value,
            usuario.Email.Value);
    }
}
