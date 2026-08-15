using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Nedis.Server.Modules.Identity.IntegrationTests.Features.Login;

[Collection(nameof(IdentityApiCollection))]
public class LoginEndpointTests(IdentityApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record AuthResponseDto(string AccessToken, Guid UserId, string Login);

    private static string UniqueLogin() => $"user_{Guid.NewGuid():N}"[..24];

    private async Task RegisterAsync(string login, string password) =>
        await fixture.Client.PostAsJsonAsync("/api/auth/register", new { login, password });

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithToken()
    {
        var login = UniqueLogin();
        await RegisterAsync(login, "password123");

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { login, password = "password123" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var login = UniqueLogin();
        await RegisterAsync(login, "password123");

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { login, password = "wrong-password" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownLogin_Returns401_NotServerError()
    {
        // Сквозная регрессия end-to-end: тот же баг тайминг-маскировки, что и в
        // юнит-тестах LoginService, но здесь проверяем весь путь через HTTP —
        // включая сериализацию и то, что реальный конвейер не превращает
        // необработанное исключение в неожиданный 500.
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login", new { login = UniqueLogin(), password = "whatever12" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
