var builder = DistributedApplication.CreateBuilder(args);

var bd = builder
    .AddSqlServer("dbserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("db");

builder.AddProject<Projects.Api>("api").WithExternalHttpEndpoints().WithReference(bd).WaitFor(bd);

builder.Build().Run();
