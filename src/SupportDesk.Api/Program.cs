var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Признак работоспособности для проверки запуска, доступен без входа.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
