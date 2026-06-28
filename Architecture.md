# Guía de Arquitectura Limpia (Single Project) - C# y .NET 10 (Direct DbContext, Minimal APIs, Bogus y Pruebas con Aspire + Respawn)

Este documento contiene la estructura, reglas y el estado de implementación del proyecto de Arquitectura Limpia unificado en un único proyecto Web API (`Api`). En este diseño, los **Slices Verticales** inyectan y consumen el **DbContext** directamente, evitando sobrecargas de capas adicionales (como repositorios genéricos o específicos) innecesarias.

---

## 1. Estructura del Proyecto

El código está organizado de la siguiente manera dentro del espacio de trabajo:

```
TrabajoServer1/
├── Api/                   # Proyecto Web API (Producción)
│   ├── Domain/            # Reglas de negocio e interfaces puras (Sin dependencias externas)
│   │   ├── Common/        # Clases base reutilizables (Entity, ValidationException)
│   │   ├── ValueObjects/  # Objetos de Valor usando Vogen (Nombre, Apellido, Email, UserId)
│   │   └── Entities/      # Entidades del Dominio (Usuario)
│   ├── Application/       # Casos de uso (CQRS y Slices Verticales en archivo único)
│   │   └── Features/      # Características organizadas por slices verticales (CreateUser, GetUserById, GetUsers)
│   └── Infrastructure/    # Comunicaciones y adaptadores externos (Data DbContext)
│
├── Api.AppHost/           # Orquestador Aspire principal para Ejecución Local (API + DB)
│
├── Api.Tests.AppHost/     # Orquestador Aspire dedicado exclusivamente para Pruebas (Solo DB)
│
└── Api.Tests/             # Proyecto de Pruebas Funcionales e Integración (NUnit)
    ├── Features/          # Pruebas organizadas por tipo de operación
    │   ├── Commands/      # Pruebas de integración de comandos (CreateUserTests.cs)
    │   └── Queries/       # Pruebas de integración de consultas (GetUserByIdTests.cs, GetUsersTests.cs)
    ├── Factories/         # Generación de datos de prueba aislados con Bogus (UserTestFactory.cs)
    ├── TestDatabaseFixture.cs # Inicialización global de contenedores Aspire, HttpClient y Respawn
    └── BaseIntegrationTest.cs # Clase base para reiniciar BD y proveer canal MediatR y cliente HTTP
```

---

## 2. Reglas de Diseño

### A. Capa de Dominio (Domain)
- **Independencia**: No depende de ninguna otra capa ni de librerías de infraestructura externa.
- **Entidades**: Heredan de la clase base genérica `Entity<TId>` para abstraer campos reutilizables (como el `Id`) sin contener lógica de infraestructura.
- **Objetos de Valor**: Representados mediante la librería **Vogen** como structs de solo lectura (`readonly struct`), asegurando encapsulamiento y validación automática durante la creación (`From`).

### B. Capa de Aplicación (Application)
- **Feature en Archivo Único**: Cada vertical slice (Feature) contiene en un **único archivo** C#:
  1. El registro del **Endpoint de la Minimal API** (ej. `MapCreateUser`).
  2. Los **DTOs** de entrada/salida y registros de paginación (Mapeos).
  3. El **Comando o Consulta** CQRS, el cual expone un método estático `public static void Validate(TCommand command)`.
  4. El **Manejador (Handler)** de MediatR, el cual interactúa directamente con el DbContext.
- **Validación Estática Acumulativa (`public static void Validate`)**: Los comandos/consultas exponen un método estático de validación que acumula los errores utilizando `TryFrom` de Vogen y arroja una excepción `ValidationException` (de tipo Domain) si existen fallas. El handler ejecuta esta validación al inicio de su ejecución, garantizando que los comandos procesados sean siempre válidos.
- **Sin Repositorio de Dominio**: El DbContext de EF Core ya actúa como una abstracción (unidad de trabajo/patrón repositorio). Inyectarlo directamente en los handlers elimina la redundancia y simplifica las consultas.

### C. Capa de Infraestructura (Infrastructure)
- **Data (Base de Datos)**: Mapeo físico mediante **EF Core (SQL Server)**. Se incluye configuración de Value Converters en EF Core para la persistencia e hidratación de los Objetos de Valor de Vogen.
- **.NET Aspire 13+**: Orquestación e inyección automática del cliente de base de datos SQL Server utilizando componentes nativos de Aspire.
- **Sembrado (Seeds) con Bogus**: Generación automatizada de 15 usuarios ficticios realistas en español en el arranque si la base de datos está vacía.

### D. Pruebas Funcionales e Integración (Api.Tests)
- **Aislamiento de Infraestructura de Pruebas**: Utiliza un AppHost de pruebas independiente (`Api.Tests.AppHost`) para instanciar dinámicamente un contenedor SQL Server dedicado a los tests, garantizando que los entornos de desarrollo y de pruebas no interfieran entre sí.
- **Ciclo de Vida de Base de Datos y Respawn**:
  1. `TestDatabaseFixture` inicializa el AppHost de pruebas y expone la base de datos limpia de SQL Server.
  2. Captura la cadena de conexión generada por el orquestador Aspire y la expone como variable de entorno `ConnectionStrings__db` antes de levantar `WebApplicationFactory`.
  3. Levanta la API en memoria con `WebApplicationFactory<Api.Program>` de ASP.NET Core y expone un `HttpClient` para llamadas HTTP reales.
  4. Configura `Respawner` (Respawn) apuntando a la conexión existente del DbContext para limpiar el esquema de base de datos antes de cada test.
- **Mapeo y Ejecución de Pruebas**:
  - **Pruebas de CQRS (MediatR)**: Los tests heredan de `BaseIntegrationTest` y envían comandos/consultas a través de `ISender` (MediatR), validando la funcionalidad del Slice de principio a fin (reglas, persistencia).
  - **Pruebas de Endpoint (HTTP)**: Emplean el `HttpClient` inyectado para probar routing, serialización, deserialización JSON y códigos de estado HTTP (p. ej., `200 OK`, `404 NotFound`, `400 BadRequest`).
  - **Edge Cases**: Validación estricta de casos límite como unicidad insensible a mayúsculas, límites de longitud (max/excedidos), GUIDs vacíos, páginas negativas y base de datos vacía.

---

## 3. Estado de Implementación

| Componente | Tipo | Estado | Detalles |
| :--- | :--- | :--- | :--- |
| **Vogen Config/Nuget** | Configuración | 🟢 Completado | Instalado el paquete NuGet `Vogen` (versión 8.0.5). |
| **MediatR Config/Nuget** | Configuración | 🟢 Completado | Instalado el paquete NuGet `MediatR` (versión 14.1.0). |
| **Bogus Config/Nuget** | Configuración | 🟢 Completado | Instalado el paquete NuGet `Bogus` (versión 35.6.5). |
| **Shouldly Config/Nuget** | Configuración | 🟢 Completado | Instalado el paquete NuGet `Shouldly` (versión 4.3.0) para aserciones semánticas. |
| **Respawn Config/Nuget** | Configuración | 🟢 Completado | Instalado el paquete NuGet `Respawn` (versión 7.0.0) para limpieza de BD. |
| **EF Core SQL Server (Aspire)**| Configuración | 🟢 Completado | Instalado `Aspire.Microsoft.EntityFrameworkCore.SqlServer` (versión 13.4.6). |
| **Clase Base Entity** | Dominio | 🟢 Completado | Definición de `Entity<TId>` genérica en [Entity.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Domain/Common/Entity.cs). |
| **ValidationException** | Dominio | 🟢 Completado | Excepción personalizada para agrupar errores de validación en [ValidationException.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Domain/Common/ValidationException.cs). |
| **Objetos de Valor** | Dominio | 🟢 Completado | `UserId`, `Nombre`, `Apellido` y `Email` con Vogen en [ValueObjects/](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Domain/ValueObjects/). |
| **Entidad Usuario** | Dominio | 🟢 Completado | Entidad `Usuario` en [Usuario.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Domain/Entities/Usuario.cs). |
| **Feature: CreateUser** | Aplicación / API | 🟢 Completado | Endpoint, Comando `CreateUserCommand` y Handler en [CreateUser.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Features/Users/CreateUser/CreateUser.cs). |
| **Feature: GetUserById** | Aplicación / API | 🟢 Completado | Endpoint, DTOs y Handler en [GetUserById.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Features/Users/GetUserById/GetUserById.cs). |
| **Feature: GetUsers (Paginada)**| Aplicación / API | 🟢 Completado | Endpoint, Consulta `GetUsersQuery` y Handler en [GetUsers.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Features/Users/GetUsers/GetUsers.cs). |
| **Feature: UpdateUser** | Aplicación / API | 🟢 Completado | Endpoint, Comando `UpdateUserCommand` con `public static void Validate` y Handler en [UpdateUser.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Features/Users/UpdateUser/UpdateUser.cs). |
| **Feature: DeleteUser (Soft Delete)** | Aplicación / API | 🟢 Completado | Endpoint, Comando `DeleteUserCommand` y Handler en [DeleteUser.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Features/Users/DeleteUser/DeleteUser.cs). |
| **Persistencia de Datos (EF)** | Infraestructura | 🟢 Completado | Configurado `UsuariosDbContext` en [Data/](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Infrastructure/Data/), incluyendo filtros globales para Soft Delete e índice único filtrado por `IsDeleted`. |
| **Pipeline Behaviors** | Aplicación | 🟢 Completado | Registrados `LoggingBehavior` y `PerformanceBehavior` en [Behaviors/](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Application/Behaviors/) para rastreo de solicitudes y auditoría de tiempos. |
| **Sembrado de Datos (Bogus)** | Infraestructura | 🟢 Completado | Sembrado automático `DbInitializer` en [DbInitializer.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Infrastructure/Data/DbInitializer.cs) con lógica resiliente de base de datos. |
| **Integración en Program** | API | 🟢 Completado | Mapeo de Minimal APIs en [Program.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Program.cs). |
| **AppHost de Pruebas** | Pruebas | 🟢 Completado | Creado `Api.Tests.AppHost` que define el contenedor de SQL Server de pruebas. |
| **Proyecto de Pruebas (NUnit)**| Pruebas | 🟢 Completado | Creado `Api.Tests` integrado con `WebApplicationFactory`, `MediatR` y `Respawn`. |
| **Pruebas de Features** | Pruebas | 🟢 Completado | Desarrolladas pruebas automatizadas en `Commands/` y `Queries/` usando `Shouldly`, `Bogus` y `HttpClient` para aserciones a nivel HTTP, incluyendo edge cases. |

---

## 4. Detalles de las Implementaciones Clave

### Resiliencia de Arranque en Base de Datos
Para dar soporte al inicio asíncrono y lento del contenedor de SQL Server de pruebas, añadimos un bucle de reintento resiliente en el inicializador [DbInitializer.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api/Infrastructure/Data/DbInitializer.cs#L15-L30) de la base de datos:
```csharp
var databaseReady = false;
var retries = 40;
while (!databaseReady && retries > 0)
{
    try
    {
        await context.Database.EnsureCreatedAsync();
        databaseReady = true;
    }
    catch (Exception)
    {
        retries--;
        if (retries == 0) throw;
        await Task.Delay(1000);
    }
}
```

### Configuración del Test Fixture Global con Aspire y Respawn
En [TestDatabaseFixture.cs](file:///C:/Users/Marcelo/Desktop/TrabajoServer1/Api.Tests/TestDatabaseFixture.cs), levantamos el orquestador de pruebas de Aspire, capturamos su cadena de conexión y la exponemos al entorno de la API antes de crear el servidor de MVC Testing:
```csharp
[OneTimeSetUp]
public async Task RunBeforeAnyTests()
{
    // 1. Iniciar el AppHost de pruebas (SQL Server en Docker)
    var appHostTestingBuilder = await Aspire.Hosting.Testing.DistributedApplicationTestingBuilder.CreateAsync<Projects.Api_Tests_AppHost>();
    _appHost = await appHostTestingBuilder.BuildAsync();
    await _appHost.StartAsync();

    // 2. Obtener cadena de conexión dinámica
    _connectionString = await _appHost.GetConnectionStringAsync("db");

    // 3. Establecer variable de entorno para la configuración de la WebAPI
    Environment.SetEnvironmentVariable("ConnectionStrings__db", _connectionString);

    // 4. Levantar la factoría web
    _factory = new WebApplicationFactory<Api.Program>();
    _ = _factory.Services; // Desencadena el arranque resiliente y la creación del esquema

    // 5. Configurar Respawn usando la conexión validada del DbContext
    using (var scope = _factory.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var dbConnection = dbContext.Database.GetDbConnection();
        if (dbConnection.State != System.Data.ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        _respawner = await Respawner.CreateAsync(dbConnection, new RespawnerOptions
        {
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" },
            DbAdapter = DbAdapter.SqlServer
        });
    }
}
```

### Integración de HttpClient para Pruebas a Nivel de Endpoint
Para garantizar que se validen los códigos de estado HTTP y la serialización JSON, la clase base `BaseIntegrationTest` expone un `HttpClient` listo para usar en cada test:

```csharp
public abstract class BaseIntegrationTest
{
    private IServiceScope? _scope;

    protected ISender Sender { get; private set; } = default!;
    protected IServiceProvider Services { get; private set; } = default!;
    protected HttpClient HttpClient { get; private set; } = default!;

    [SetUp]
    public async Task Setup()
    {
        await TestDatabaseFixture.ResetDatabaseAsync();

        _scope = TestDatabaseFixture.Services.CreateScope();
        Services = _scope.ServiceProvider;
        Sender = Services.GetRequiredService<ISender>();

        // Cliente HTTP inyectado para llamadas a endpoints
        HttpClient = TestDatabaseFixture.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _scope?.Dispose();
        HttpClient.Dispose();
    }
}
```

### MediatR Pipeline Behaviors (Logging y Performance)
Se configuraron dos comportamientos de canalización (*Pipeline Behaviors*) genéricos para encapsular los aspectos transversales de auditoría y análisis de tiempos sin acoplar los handlers:

#### LoggingBehavior
Registra de forma automática cuándo inicia y termina cada `Command`/`Query` junto con los parámetros serializados en JSON:
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ...
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellation)
    {
        _logger.LogInformation("Manejando solicitud {RequestName}. Datos: {RequestData}", typeof(TRequest).Name, JsonSerializer.Serialize(request));
        try
        {
            var response = await next();
            _logger.LogInformation("Solicitud {RequestName} completada con éxito.", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error al manejar la solicitud {RequestName}.", typeof(TRequest).Name);
            throw;
        }
    }
}
```

#### PerformanceBehavior
Mide el tiempo transcurrido de ejecución del handler con un `Stopwatch`. Si la solicitud tarda más del umbral configurado de **500 ms**, emite automáticamente un log de advertencia (`LogWarning`) para identificar cuellos de botella de rendimiento:
```csharp
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ...
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellation)
    {
        _timer.Start();
        var response = await next();
        _timer.Stop();

        var elapsed = _timer.ElapsedMilliseconds;
        _logger.LogInformation("Métrica de Rendimiento: La solicitud {RequestName} tardó {Elapsed} ms.", typeof(TRequest).Name, elapsed);

        if (elapsed > 500)
        {
            _logger.LogWarning("Advertencia de Rendimiento: Solicitud lenta detectada. {RequestName} tomó {Elapsed} ms.", typeof(TRequest).Name, elapsed);
        }
        return response;
    }
}
```

