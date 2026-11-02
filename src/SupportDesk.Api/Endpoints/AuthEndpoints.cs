using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Data;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Security;

namespace SupportDesk.Api.Endpoints;

// В модели запроса нет полей автора и роли: субъект определяет сервер (D-01).
public record LoginRequest(string? Login, string? Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Вход доступен без сессии (D-01).
        app.MapPost("/auth/login", async (
            LoginRequest request,
            AppDbContext db,
            SessionManager sessions,
            CancellationToken ct) =>
        {
            // Один и тот же ответ на неверный пароль и на несуществующий логин (SR-06).
            IResult invalid = Results.Json(
                new { error = "invalid login or password" },
                statusCode: StatusCodes.Status401Unauthorized);

            if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
            {
                return invalid;
            }

            Account? account = await db.Accounts
                .FirstOrDefaultAsync(a => a.Login == request.Login, ct);

            if (account is null || !PasswordHasher.Verify(request.Password, account.PasswordHash))
            {
                return invalid;
            }

            Session session = await sessions.CreateAsync(account, ct);
            return Results.Ok(new { sessionId = session.Id, expiresAt = session.ExpiresAt });
        }).AllowAnonymous();

        // Пробная защищенная точка входа: показывает субъекта, которого определил сервер.
        // Логин не возвращается, только отображаемое имя и роль (SR-06).
        app.MapGet("/me", (CurrentUser currentUser) =>
        {
            Account account = currentUser.Account!;
            return Results.Ok(new { displayName = account.DisplayName, role = account.Role.ToString() });
        });
    }
}
