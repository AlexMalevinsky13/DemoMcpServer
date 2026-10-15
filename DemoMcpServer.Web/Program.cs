using DemoMcpServer.Web.Services;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SqlQueryValidator>();
builder.Services.AddScoped<IDatabaseSchemaService, SqlServerSchemaService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    ServerTime = DateTimeOffset.UtcNow
}));

app.MapMcp("/");
app.MapMcp("/v7");

app.Run();