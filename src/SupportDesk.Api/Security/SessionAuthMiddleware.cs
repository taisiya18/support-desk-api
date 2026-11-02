using Microsoft.AspNetCore.Authorization;
using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Security;

// Общая проверка сессии до вызова обработчика (D-01, SR-06).
// Безопасное значение по умолчанию: точка входа требует входа,
// пока она явно не помечена как доступная без него.
public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAuthMiddleware> _logger;

    public SessionAuthMiddleware(RequestDelegate next, ILogger<SessionAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, SessionManager sessions, CurrentUser currentUser)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        string? sessionId = ReadBearerToken(context);
        if (sessionId is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        Account? account;
        try
        {
            account = await sessions.FindAccountAsync(sessionId, context.RequestAborted);
        }
        catch (Exception ex)
        {
            // Хранилище недоступно или поиск не удался: обработчик не вызывается, данные не возвращаются.
            _logger.LogError(ex, "Не удалось проверить сессию.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "service unavailable" });
            return;
        }

        if (account is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        currentUser.Account = account;
        await _next(context);
    }

    // Идентификатор сессии берется только из одного заголовка Authorization вида "Bearer <id>".
    private static string? ReadBearerToken(HttpContext context)
    {
        const string prefix = "Bearer ";
        var header = context.Request.Headers["Authorization"];
        if (header.Count != 1)
        {
            return null;
        }

        string? value = header[0];
        if (value is null || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        string token = value[prefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { error = "authentication required" });
    }
}
