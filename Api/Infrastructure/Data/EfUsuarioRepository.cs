using Api.Domain.Entities;
using Api.Domain.Repositories;
using Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

/// <summary>
/// Implementación de IUsuarioRepository utilizando EF Core con SQL Server.
/// </summary>
public class EfUsuarioRepository : IUsuarioRepository
{
    private readonly UsuariosDbContext _context;

    public EfUsuarioRepository(UsuariosDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);
        return !exists;
    }
}
