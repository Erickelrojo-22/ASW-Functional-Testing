using Vogen;

namespace Api.Domain.ValueObjects;

[ValueObject(typeof(string))]
public partial struct Apellido
{
    private static Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Validation.Invalid(
                "El apellido no puede estar vacío o contener solo espacios en blanco."
            )
            : Validation.Ok;
}
