using System.Net;
using System.Text;
using System.Text.Json;

namespace SupportDesk.Api.Tests;

// Проверки входа и общей проверки сессии: SR-06, D-01.
public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_IsAvailableWithoutLogin()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"ok\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_WithoutAuthorizationHeader_IsRejected()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithUnknownSessionId_IsRejected()
    {
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer 00112233445566778899aabbccddeeff");

        HttpResponseMessage response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithSessionIdWithoutBearerPrefix_IsRejected()
    {
        using HttpClient client = _factory.CreateClient();
        string sessionId = await LoginAndGetSessionIdAsync(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sessionId);

        HttpResponseMessage response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Критерий приемки SR-06: ответы на неверный пароль и на несуществующий логин совпадают.
    [Fact]
    public async Task Login_WithWrongPasswordAndWithUnknownLogin_ReturnsIdenticalResponses()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage wrongPassword = await client.PostAsync(
            "/auth/login", JsonBody(LoginBody(ApiFactory.UserLogin, "wrong-password")));
        HttpResponseMessage unknownLogin = await client.PostAsync(
            "/auth/login", JsonBody(LoginBody("no-such-login", "wrong-password")));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(wrongPassword.StatusCode, unknownLogin.StatusCode);
        Assert.Equal(
            await wrongPassword.Content.ReadAsStringAsync(),
            await unknownLogin.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithValidCredentials_IssuesSessionForEightHours()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/auth/login", JsonBody(LoginBody(ApiFactory.UserLogin, ApiFactory.UserPassword)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string sessionId = document.RootElement.GetProperty("sessionId").GetString()!;
        DateTimeOffset expiresAt = document.RootElement.GetProperty("expiresAt").GetDateTimeOffset();

        // 128 бит в шестнадцатеричной записи.
        Assert.Matches("^[0-9a-f]{32}$", sessionId);
        Assert.InRange((expiresAt - DateTimeOffset.UtcNow).TotalHours, 7.9, 8.0);
    }

    [Fact]
    public async Task Me_WithValidSession_ReturnsDisplayNameAndRoleWithoutLogin()
    {
        using HttpClient client = _factory.CreateClient();
        string sessionId = await LoginAndGetSessionIdAsync(client);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sessionId}");

        HttpResponseMessage response = await client.GetAsync("/me");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ApiFactory.UserDisplayName, body);
        Assert.Contains("\"role\":\"User\"", body);
        Assert.DoesNotContain(ApiFactory.UserLogin, body);
    }

    private async Task<string> LoginAndGetSessionIdAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsync(
            "/auth/login", JsonBody(LoginBody(ApiFactory.UserLogin, ApiFactory.UserPassword)));

        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("sessionId").GetString()!;
    }

    private static string LoginBody(string login, string password) =>
        JsonSerializer.Serialize(new { login, password });

    private static StringContent JsonBody(string body) =>
        new(body, Encoding.UTF8, "application/json");
}
