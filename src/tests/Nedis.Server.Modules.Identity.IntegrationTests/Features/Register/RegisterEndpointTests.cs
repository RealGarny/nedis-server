using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Nedis.Server.Modules.Identity.IntegrationTests.Features.Register;

[Collection(nameof(IdentityApiCollection))]
public class RegisterEndpointTests(IdentityApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record AuthResponseDto(string AccessToken, Guid UserId, string Login);

    private static string UniqueLogin() => $"user_{Guid.NewGuid():N}"[..24];

    [Fact]
    public async Task Register_NewLogin_Returns200WithToken()
    {
        var login = UniqueLogin();

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/register", new { login, password = "password123" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal(login, body.Login);
    }

    [Fact]
    public async Task Register_DuplicateLogin_ReturnsBadRequest()
    {
        var login = UniqueLogin();
        await fixture.Client.PostAsJsonAsync("/api/auth/register", new { login, password = "password123" }, TestContext.Current.CancellationToken);

        var second = await fixture.Client.PostAsJsonAsync("/api/auth/register", new { login, password = "password456" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Theory]
    [InlineData("ab", "password123")]       // login is shorter that 3 chars
    [InlineData("valid-login", "short")]    // password is shorter than 8 chars
    [InlineData("really-really-really-really-long.", "short")]    // login is longer than 32 characters
    public async Task Register_InvalidInput_ReturnsBadRequest(string login, string password)
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/auth/register", new { login, password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
