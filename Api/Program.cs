var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registrar dependencias de la arquitectura limpia
builder.AddSqlServerDbContext<Api.Infrastructure.Data.UsuariosDbContext>("db");
builder.Services.AddScoped<Api.Domain.Repositories.IUsuarioRepository, Api.Infrastructure.Data.EfUsuarioRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Inicializar y sembrar base de datos con Bogus
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Api.Infrastructure.Data.UsuariosDbContext>();
    await Api.Infrastructure.Data.DbInitializer.InitializeAsync(context);
}

app.Run();
