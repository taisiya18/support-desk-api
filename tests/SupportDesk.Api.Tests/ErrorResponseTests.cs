using System.Net;
using System.Text;

namespace SupportDesk.Api.Tests;

// Проверка общего ответа об ошибке: D-03, раздел «Поведение при сбое».
// Закрепляет исправление дефекта, описанного в docs/evidence.md.
public class ErrorResponseTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ErrorResponseTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithMalformedJson_ReturnsGenericErrorWithoutDetails()
    {
        using HttpClient client = _factory.CreateClient();
        StringContent malformed = new("{broken", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/auth/login", malformed);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("{\"error\":\"bad request\"}", body);

        // В ответе не должно быть ни типа исключения, ни трассировки, ни путей к исходникам.
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("SupportDesk.Api", body);
        Assert.DoesNotContain("   at ", body);
    }
}
