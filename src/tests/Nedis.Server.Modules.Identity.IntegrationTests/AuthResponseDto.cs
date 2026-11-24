namespace Nedis.Server.Modules.Identity.IntegrationTests;

internal sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    Guid UserId,
    string Login);
