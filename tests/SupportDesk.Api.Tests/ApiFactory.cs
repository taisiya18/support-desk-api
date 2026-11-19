using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SupportDesk.Api.Tests;

// Поднимает приложение в памяти с отдельной временной базой и собственной учебной
// записью, чтобы тесты не зависели от секретов на машине разработчика.
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string UserLogin = "tests-user";
    public const string UserPassword = "tests-user-password";
    public const string UserDisplayName = "Tests User";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"supportdesk-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development выбран намеренно, тк в нем ASP.NET Core включает страницу ошибок
        // для разработчика. Проверка ответа об ошибке имеет смысл только в этом окружении.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = $"Data Source={_databasePath}",
                ["SeedAccounts:0:Login"] = UserLogin,
                ["SeedAccounts:0:DisplayName"] = UserDisplayName,
                ["SeedAccounts:0:Role"] = "User",
                ["SeedAccounts:0:Password"] = UserPassword
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // SQLite в режиме wal создает рядом с базой еще два файла.
        foreach (string suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                File.Delete(_databasePath + suffix);
            }
            catch (IOException)
            {
                // Временный файл мог остаться занятым, для теста это не важно.
            }
        }
    }
}
