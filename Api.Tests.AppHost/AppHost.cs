var builder = DistributedApplication.CreateBuilder(args);

// Crear servidor SQL Server de prueba.
// Session indica que se recrea cada vez que inicia una sesión de prueba.
builder.AddSqlServer("dbserver").AddDatabase("db");

builder.Build().Run();
