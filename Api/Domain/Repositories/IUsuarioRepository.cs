using Api.Domain.Entities;
using Api.Domain.ValueObjects;

namespace Api.Domain.Repositories;

/// <summary>
/// Interfaz para el repositorio de Usuarios.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default);
}
