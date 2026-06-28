namespace Api.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// </summary>
/// <typeparam name="TId">Tipo del identificador de la entidad.</typeparam>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }
}
