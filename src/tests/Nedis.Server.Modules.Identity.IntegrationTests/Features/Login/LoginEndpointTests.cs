using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Nedis.Server.Modules.Identity.IntegrationTests.Features.Login;

[Collection(nameof(IdentityApiCollection))]
public class LoginEndpointTests(IdentityApiFixture fixture)
{

    private static string UniqueLogin() => $"user_{Guid.NewGuid():N}"[..24];

    private async Task RegisterAsync(string login, string password)
    {
        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { login, password },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithToken()
    {
        var login = UniqueLogin();
        await RegisterAsync(login, "password123");

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { login, password = "password123" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //response body checks
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.True(body.ExpiresIn > 0);
        Assert.NotEqual(Guid.Empty, body.UserId);
        Assert.Equal(login, body.Login);
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
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login", new { login = UniqueLogin(), password = "whatever12" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
