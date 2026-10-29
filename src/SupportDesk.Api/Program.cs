using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDb")));

var app = builder.Build();

// Создание базы и начальных учетных записей при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    AccountSeeder.Seed(db, builder.Configuration, logger);
}

// Признак работоспособности для проверки запуска, доступен без входа.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
