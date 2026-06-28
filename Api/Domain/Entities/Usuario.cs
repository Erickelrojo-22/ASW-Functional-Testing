using Api.Domain.Common;
using Api.Domain.ValueObjects;

namespace Api.Domain.Entities;

/// <summary>
/// Entidad del dominio que representa un Usuario.
/// </summary>
public class Usuario : Entity<UserId>
{
    public Nombre Nombre { get; private set; }
    public Apellido Apellido { get; private set; }
    public Email Email { get; private set; }

    // Requerido para ORMs como EF Core y serialización
#pragma warning disable CS8618
    private Usuario() { }
#pragma warning restore CS8618

    public Usuario(UserId id, Nombre nombre, Apellido apellido, Email email) : base(id)
    {
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }

    /// <summary>
    /// Factory method para crear una nueva instancia de Usuario.
    /// </summary>
    public static Usuario Create(UserId id, Nombre nombre, Apellido apellido, Email email)
    {
        return new Usuario(id, nombre, apellido, email);
    }

    /// <summary>
    /// Método para actualizar la información básica del usuario.
    /// </summary>
    public void Update(Nombre nombre, Apellido apellido, Email email)
    {
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
