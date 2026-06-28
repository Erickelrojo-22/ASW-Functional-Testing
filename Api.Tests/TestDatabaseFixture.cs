using Api.Infrastructure.Data;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Respawn;

namespace Api.Tests;

/// <summary>
/// Configuración global de la base de datos de pruebas usando .NET Aspire y Respawn.
/// </summary>
[SetUpFixture]
public class TestDatabaseFixture
{
    private static Aspire.Hosting.DistributedApplication? _appHost;
    private static WebApplicationFactory<Api.Program>? _factory;
    private static string? _connectionString;
    private static Respawner? _respawner;

    public static IServiceProvider Services =>
        _factory?.Services
        ?? throw new InvalidOperationException("Los servicios no están inicializados.");

    public static HttpClient CreateClient() => _factory?.CreateClient()
        ?? throw new InvalidOperationException("La fábrica web no está inicializada.");

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        // 1. Iniciar el AppHost de pruebas (SQL Server Container)
        var appHostTestingBuilder =
            await Aspire.Hosting.Testing.DistributedApplicationTestingBuilder.CreateAsync<Projects.Api_Tests_AppHost>();
        _appHost = await appHostTestingBuilder.BuildAsync();
        await _appHost.StartAsync();

        // 2. Obtener la cadena de conexión dinámica generada por el contenedor de Aspire
        _connectionString = await _appHost.GetConnectionStringAsync("db");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "No se pudo obtener la cadena de conexión del contenedor de SQL Server de Aspire."
            );
        }

        // Establecer variable de entorno para que el WebApplicationBuilder de la API la lea al arrancar
        Environment.SetEnvironmentVariable("ConnectionStrings__db", _connectionString);

        // 3. Configurar WebApplicationFactory apuntando a la base de datos dinámica
        _factory = new WebApplicationFactory<Api.Program>();

        // 4. Provocar la inicialización del host (lo que ejecuta Program.cs y el DbInitializer de forma resiliente)
        _ = _factory.Services;

        // Esperar a que la base de datos esté totalmente creada y configurar Respawner
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
            var dbReady = false;
            var retries = 40;
            while (!dbReady && retries > 0)
            {
                try
                {
                    _ = await dbContext.Usuarios.AnyAsync();
                    dbReady = true;
                }
                catch (Exception)
                {
                    retries--;
                    if (retries == 0)
                        throw;
                    await Task.Delay(1000);
                }
            }

            // Configurar Respawner usando la conexión existente y validada del DbContext
            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State != System.Data.ConnectionState.Open)
            {
                await dbConnection.OpenAsync();
            }

            _respawner = await Respawner.CreateAsync(
                dbConnection,
                new RespawnerOptions
                {
                    TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" },
                    DbAdapter = DbAdapter.SqlServer,
                }
            );
        }
    }

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        if (_appHost != null)
        {
            await _appHost.DisposeAsync();
        }
    }

    public static async Task ResetDatabaseAsync()
    {
        if (_respawner != null)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State != System.Data.ConnectionState.Open)
            {
                await dbConnection.OpenAsync();
            }
            await _respawner.ResetAsync(dbConnection);
        }
    }
}
