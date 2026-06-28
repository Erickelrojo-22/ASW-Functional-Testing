using System.Collections.Concurrent;
using Api.Domain.Entities;
using Api.Domain.Repositories;
using Api.Domain.ValueObjects;

namespace Api.Infrastructure.Data;

/// <summary>
/// Implementación en memoria del repositorio de Usuarios para simplificar la infraestructura inicial.
/// </summary>
public class InMemoryUsuarioRepository : IUsuarioRepository
{
    private static readonly ConcurrentDictionary<UserId, Usuario> _usuarios = new();

    public Task<Usuario?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _usuarios.TryGetValue(id, out var usuario);
        return Task.FromResult(usuario);
    }

    public Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _usuarios.TryAdd(usuario.Id, usuario);
        return Task.CompletedTask;
    }

    public Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default)
    {
        var exists = _usuarios.Values.Any(u => u.Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(!exists);
    }
}
