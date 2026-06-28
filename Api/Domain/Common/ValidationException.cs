namespace Api.Domain.Common;

/// <summary>
/// Excepción personalizada para agrupar errores de validación en la aplicación.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Uno o más errores de validación ocurrieron.")
    {
        Errors = errors;
    }
}
