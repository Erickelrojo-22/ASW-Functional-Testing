using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Api.Application.Behaviors;

/// <summary>
/// Pipeline Behavior de MediatR para registrar logs de inicio, éxito y error de cada solicitud (Command/Query).
/// </summary>
/// <typeparam name="TRequest">Tipo de la solicitud.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        string requestData;
        try
        {
            requestData = JsonSerializer.Serialize(request);
        }
        catch (Exception)
        {
            requestData = "[No serializable]";
        }

        _logger.LogInformation(
            "Manejando solicitud {RequestName}. Datos: {RequestData}", 
            requestName, 
            requestData);

        try
        {
            var response = await next();
            
            _logger.LogInformation(
                "Solicitud {RequestName} completada con éxito.", 
                requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Ocurrió un error al manejar la solicitud {RequestName}.", 
                requestName);
            throw;
        }
    }
}
