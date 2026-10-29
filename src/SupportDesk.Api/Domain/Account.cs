namespace SupportDesk.Api.Domain;

// Роль определяется учетной записью и хранится в базе, а не берется из запроса (SR-03, D-01).
public enum Role
{
    User = 0,
    Specialist = 1
}

// Учетная запись. Пароль хранится только как медленный хеш (SR-07, D-04).
public class Account
{
    public int Id { get; set; }
    public required string Login { get; set; }
    public required string DisplayName { get; set; }
    public Role Role { get; set; }
    public required string PasswordHash { get; set; }
}
