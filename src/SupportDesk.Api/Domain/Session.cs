namespace SupportDesk.Api.Domain;

// Серверная сессия (D-01). Идентификатор случайный, срок ограничен.
// Записи создаются при входе на под-шаге 1.2.
public class Session
{
    public required string Id { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
