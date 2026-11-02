using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Data;
using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Security;

// Серверные сессии (D-01): случайный идентификатор, ограниченный срок,
// проверка по хранилищу на каждом запросе.
public class SessionManager
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    // 16 байт = 128 бит.
    private const int IdSizeBytes = 16;

    private readonly AppDbContext _db;

    public SessionManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Session> CreateAsync(Account account, CancellationToken ct)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var session = new Session
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(IdSizeBytes)).ToLowerInvariant(),
            AccountId = account.Id,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime)
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    // Возвращает учетную запись с текущей ролью из хранилища
    // либо null, если сессии нет или срок истек.
    public async Task<Account?> FindAccountAsync(string sessionId, CancellationToken ct)
    {
        Session? session = await _db.Sessions
            .Include(s => s.Account)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return session.Account;
    }
}
