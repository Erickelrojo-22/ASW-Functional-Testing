using Api.Application.Features.Users.CreateUser;
using Api.Application.Features.Users.GetUserById;
using Api.Application.Features.Users.GetUsers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registrar dependencias de la arquitectura limpia
builder.AddSqlServerDbContext<Api.Infrastructure.Data.UsuariosDbContext>("db");
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/api/usuarios/")
    {
        context.Response.Redirect("/api/usuarios", permanent: false);
        return;
    }

    await next();
});

app.MapCreateUser();
app.MapGetUserById();
app.MapGetUsers();

// Inicializar y sembrar base de datos con Bogus
using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider.GetRequiredService<Api.Infrastructure.Data.UsuariosDbContext>();
    await Api.Infrastructure.Data.DbInitializer.InitializeAsync(context);
}

app.Run();
