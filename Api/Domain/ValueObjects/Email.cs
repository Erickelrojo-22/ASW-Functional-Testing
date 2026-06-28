using System.Text.RegularExpressions;
using Vogen;

namespace Api.Domain.ValueObjects;

[ValueObject(typeof(string))]
public partial struct Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Validation.Invalid("El correo electrónico no puede estar vacío.");
        }

        return EmailRegex.IsMatch(value)
            ? Validation.Ok
            : Validation.Invalid("El correo electrónico no tiene un formato válido.");
    }
}
