using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Api.Application.Behaviors;

/// <summary>
/// Pipeline Behavior de MediatR para medir el tiempo de ejecución de las solicitudes
/// y registrar advertencias si sobrepasan un umbral definido.
/// </summary>
/// <typeparam name="TRequest">Tipo de la solicitud.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        var requestName = typeof(TRequest).Name;

        // Registrar métrica de tiempo de ejecución
        _logger.LogInformation(
            "Métrica de Rendimiento: La solicitud {RequestName} tardó {ElapsedMilliseconds} ms.", 
            requestName, 
            elapsedMilliseconds);

        // Advertencia si la solicitud supera el umbral de 500ms
        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Advertencia de Rendimiento: Solicitud lenta detectada. {RequestName} tomó {ElapsedMilliseconds} ms.",
                requestName,
                elapsedMilliseconds);
        }

        return response;
    }
}
