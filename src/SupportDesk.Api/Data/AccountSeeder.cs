using SupportDesk.Api.Configuration;
using SupportDesk.Api.Domain;
using SupportDesk.Api.Security;

namespace SupportDesk.Api.Data;

// Создание начальных учетных записей при первом запуске (D-04, допущение А4).
// Пароль в журнал не пишется; при отклонении записи журналируется только имя ключа.
public static class AccountSeeder
{
    public static void Seed(AppDbContext db, IConfiguration configuration, ILogger logger)
    {
        var inputs = configuration.GetSection("SeedAccounts").Get<List<SeedAccountInput>>() ?? new List<SeedAccountInput>();

        for (int i = 0; i < inputs.Count; i++)
        {
            SeedAccountInput input = inputs[i];
            string key = $"SeedAccounts:{i}";

            if (string.IsNullOrWhiteSpace(input.Login)
                || string.IsNullOrWhiteSpace(input.Password)
                || string.IsNullOrWhiteSpace(input.DisplayName)
                || !Enum.TryParse(input.Role, ignoreCase: true, out Role role))
            {
                logger.LogWarning("Начальная учетная запись {Key} пропущена: не заданы обязательные поля или неизвестная роль.", key);
                continue;
            }

            if (string.Equals(input.DisplayName, input.Login, StringComparison.Ordinal))
            {
                logger.LogWarning("Начальная учетная запись {Key} пропущена: отображаемое имя совпадает с логином.", key);
                continue;
            }

            if (db.Accounts.Any(a => a.Login == input.Login))
            {
                continue;
            }

            db.Accounts.Add(new Account
            {
                Login = input.Login,
                DisplayName = input.DisplayName,
                Role = role,
                PasswordHash = PasswordHasher.Hash(input.Password)
            });
        }

        db.SaveChanges();
    }
}
