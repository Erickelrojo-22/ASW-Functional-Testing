using Vogen;

namespace Api.Domain.ValueObjects;

[ValueObject(typeof(string))]
public partial struct Nombre
{
    private static Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Validation.Invalid("El nombre no puede estar vacío o contener solo espacios en blanco.")
            : Validation.Ok;
}
