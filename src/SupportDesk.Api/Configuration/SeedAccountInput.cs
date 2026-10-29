namespace SupportDesk.Api.Configuration;

// Описание начальной учебной учетной записи из конфигурации стенда (D-04).
// Значения берутся из секретов пользователя или переменных окружения, не из репозитория.
public class SeedAccountInput
{
    public string? Login { get; set; }
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}
