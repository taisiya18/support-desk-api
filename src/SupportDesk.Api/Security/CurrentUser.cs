using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Security;

// Субъект запроса, установленный общей проверкой сессии (D-01).
// Обработчики берут учетную запись и роль только отсюда, а не из данных запроса.
public class CurrentUser
{
    public Account? Account { get; set; }
}
