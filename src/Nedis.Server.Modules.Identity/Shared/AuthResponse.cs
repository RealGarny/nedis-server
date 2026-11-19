namespace Nedis.Server.Modules.Identity.Shared;

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, Guid UserId, string Login);