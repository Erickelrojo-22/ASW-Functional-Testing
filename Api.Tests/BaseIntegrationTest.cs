using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Api.Tests;

/// <summary>
/// Clase base para todas las pruebas de integración.
/// Garantiza un estado limpio de la base de datos entre ejecuciones y provee acceso al bus de MediatR.
/// </summary>
public abstract class BaseIntegrationTest
{
    private IServiceScope? _scope;

    protected ISender Sender { get; private set; } = default!;
    protected IServiceProvider Services { get; private set; } = default!;
    protected HttpClient HttpClient { get; private set; } = default!;

    [SetUp]
    public async Task Setup()
    {
        // 1. Limpiar base de datos usando Respawn
        await TestDatabaseFixture.ResetDatabaseAsync();

        // 2. Crear scope de inyección de dependencias para aislar la prueba
        _scope = TestDatabaseFixture.Services.CreateScope();
        Services = _scope.ServiceProvider;
        Sender = Services.GetRequiredService<ISender>();

        // 3. Crear cliente HTTP
        HttpClient = TestDatabaseFixture.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _scope?.Dispose();
        HttpClient.Dispose();
    }
}
