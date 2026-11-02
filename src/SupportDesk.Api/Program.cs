using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Data;
using SupportDesk.Api.Endpoints;
using SupportDesk.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDb")));
builder.Services.AddScoped<SessionManager>();
builder.Services.AddScoped<CurrentUser>();

var app = builder.Build();

// Создание базы и начальных учетных записей при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    AccountSeeder.Seed(db, builder.Configuration, logger);
}

// Общая проверка сессии стоит после маршрутизации и до вызова обработчиков (D-01).
app.UseRouting();
app.UseMiddleware<SessionAuthMiddleware>();

// Признак работоспособности для проверки запуска, доступен без входа.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapAuthEndpoints();

app.Run();
